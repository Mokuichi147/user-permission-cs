//! ユーザー管理 FFI (`db.users()` 相当)。

use std::os::raw::c_char;

use user_permission_core::UserUpdate;

use crate::{db_of, err_to_cstr, opt_str, req_str, run_json, tri_bool, DbHandle};

/// ユーザーを作成する (`ok`: User)。
#[no_mangle]
pub unsafe extern "C" fn up_users_create(
    handle: *mut DbHandle,
    username: *const c_char,
    password: *const c_char,
    display_name: *const c_char,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let username = req_str(username);
    let password = req_str(password);
    let display_name = req_str(display_name);
    let token = opt_str(token);
    run_json(async move {
        db.users()
            .create(&username, &password, &display_name, token.as_deref())
            .await
    })
}

/// ID でユーザーを取得する (`ok`: User | null)。
#[no_mangle]
pub unsafe extern "C" fn up_users_get_by_id(
    handle: *mut DbHandle,
    user_id: i64,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.users().get_by_id(user_id, token.as_deref()).await })
}

/// ユーザー名でユーザーを取得する (`ok`: User | null)。
#[no_mangle]
pub unsafe extern "C" fn up_users_get_by_username(
    handle: *mut DbHandle,
    username: *const c_char,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let username = req_str(username);
    let token = opt_str(token);
    run_json(async move {
        db.users()
            .get_by_username(&username, token.as_deref())
            .await
    })
}

/// 全ユーザーを取得する (`ok`: [User])。
#[no_mangle]
pub unsafe extern "C" fn up_users_list_all(
    handle: *mut DbHandle,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.users().list_all(token.as_deref()).await })
}

/// ユーザーを更新する。文字列引数は null で「変更なし」。`is_active` は -1=変更なし / 0=false / 1=true。
#[no_mangle]
pub unsafe extern "C" fn up_users_update(
    handle: *mut DbHandle,
    user_id: i64,
    username: *const c_char,
    password: *const c_char,
    display_name: *const c_char,
    is_active: i32,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let update = UserUpdate {
        username: opt_str(username),
        password: opt_str(password),
        display_name: opt_str(display_name),
        is_active: tri_bool(is_active),
    };
    let token = opt_str(token);
    run_json(async move {
        db.users()
            .update(user_id, update, token.as_deref())
            .await
    })
}

/// ユーザーを削除する (`ok`: bool)。
#[no_mangle]
pub unsafe extern "C" fn up_users_delete(
    handle: *mut DbHandle,
    user_id: i64,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.users().delete(user_id, token.as_deref()).await })
}

/// ユーザーが管理者か判定する (`ok`: bool)。
#[no_mangle]
pub unsafe extern "C" fn up_users_is_admin(
    handle: *mut DbHandle,
    user_id: i64,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.users().is_admin(user_id, token.as_deref()).await })
}

/// 管理者フラグを設定する (`ok`: bool)。
#[no_mangle]
pub unsafe extern "C" fn up_users_set_admin(
    handle: *mut DbHandle,
    user_id: i64,
    is_admin: u8,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move {
        db.users()
            .set_admin(user_id, is_admin != 0, token.as_deref())
            .await
    })
}
