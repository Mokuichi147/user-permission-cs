//! `Database` のライフサイクルとバックエンド非依存の認証系 FFI。

use std::os::raw::c_char;
use std::sync::Mutex;
use std::time::Duration;

use user_permission_core::Database;

use crate::{db_of, err_to_cstr, ok_null, opt_str, req_str, run_json, runtime, DbHandle};

/// 接続先 (ファイルパス or `http(s)://` URL) と secret から未接続ハンドルを生成する。
/// `secret` は null 可。戻り値は `up_database_free` で破棄すること。
#[no_mangle]
pub unsafe extern "C" fn up_database_new(
    target: *const c_char,
    secret: *const c_char,
) -> *mut DbHandle {
    let handle = Box::new(DbHandle {
        target: req_str(target),
        secret: opt_str(secret),
        inner: Mutex::new(None),
    });
    Box::into_raw(handle)
}

/// ハンドルを破棄する。null 安全。
#[no_mangle]
pub unsafe extern "C" fn up_database_free(handle: *mut DbHandle) {
    if !handle.is_null() {
        drop(Box::from_raw(handle));
    }
}

/// バックエンドへ接続する。
#[no_mangle]
pub unsafe extern "C" fn up_database_connect(handle: *mut DbHandle) -> *mut c_char {
    if handle.is_null() {
        return err_to_cstr(user_permission_core::Error::InvalidArgument(
            "null database handle".into(),
        ));
    }
    let h = &*handle;
    let target = h.target.clone();
    let secret = h.secret.clone();
    match runtime().block_on(async move { Database::open(&target, secret.as_deref()).await }) {
        Ok(db) => {
            match h.inner.lock() {
                Ok(mut guard) => *guard = Some(db),
                Err(_) => {
                    return err_to_cstr(user_permission_core::Error::InvalidArgument(
                        "database lock poisoned".into(),
                    ))
                }
            }
            ok_null()
        }
        Err(e) => err_to_cstr(e),
    }
}

/// 接続を閉じる。未接続でも no-op。
#[no_mangle]
pub unsafe extern "C" fn up_database_close(handle: *mut DbHandle) -> *mut c_char {
    if handle.is_null() {
        return ok_null();
    }
    let h = &*handle;
    let db = match h.inner.lock() {
        Ok(mut guard) => guard.take(),
        Err(_) => {
            return err_to_cstr(user_permission_core::Error::InvalidArgument(
                "database lock poisoned".into(),
            ))
        }
    };
    if let Some(db) = db {
        if let Err(e) = runtime().block_on(async move { db.close().await }) {
            return err_to_cstr(e);
        }
    }
    ok_null()
}

/// ユーザー名 + パスワードでログインし、アクセストークン (`ok`: string | null) を得る。
#[no_mangle]
pub unsafe extern "C" fn up_database_login(
    handle: *mut DbHandle,
    username: *const c_char,
    password: *const c_char,
    expires_secs: u64,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let username = req_str(username);
    let password = req_str(password);
    run_json(async move {
        db.login(&username, &password, Duration::from_secs(expires_secs))
            .await
    })
}

/// サービスクライアント (client-credentials) でログインする。
#[no_mangle]
pub unsafe extern "C" fn up_database_login_service(
    handle: *mut DbHandle,
    client_id: *const c_char,
    client_secret: *const c_char,
    expires_secs: u64,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let client_id = req_str(client_id);
    let client_secret = req_str(client_secret);
    run_json(async move {
        db.login_service(&client_id, &client_secret, Duration::from_secs(expires_secs))
            .await
    })
}

/// トークンを検証してユーザーを解決する (無効・期限切れ・サービストークン・null は `ok`: null)。
#[no_mangle]
pub unsafe extern "C" fn up_database_verify_token_and_get_user(
    handle: *mut DbHandle,
    token: *const c_char,
) -> *mut c_char {
    let Some(token) = opt_str(token) else {
        return ok_null();
    };
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    run_json(async move { db.verify_token_and_get_user(&token).await })
}

/// 管理者が不在なら作成して昇格する (リレーでは `ok`: null)。
#[no_mangle]
pub unsafe extern "C" fn up_database_bootstrap_admin_if_needed(
    handle: *mut DbHandle,
    username: *const c_char,
    password: *const c_char,
    display_name: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let username = req_str(username);
    let password = req_str(password);
    let display_name = req_str(display_name);
    run_json(async move {
        db.bootstrap_admin_if_needed(&username, &password, &display_name)
            .await
    })
}
