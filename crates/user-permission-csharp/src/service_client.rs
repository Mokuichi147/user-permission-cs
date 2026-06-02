//! サービスクライアント (machine-to-machine) 管理 FFI。
//!
//! 管理操作 (create / list / get / delete / rotate) はローカルバックエンド専用。
//! リレー (URL) backend で呼ぶとコア側がエラーを返す。

use std::os::raw::c_char;

use serde_json::json;
use user_permission_core::Error;

use crate::{db_of, err_to_cstr, ok_null, opt_str, req_str, run_json, DbHandle};

/// JSON 配列文字列をスコープのベクタへ変換する。
unsafe fn parse_scopes(scopes_json: *const c_char) -> Result<Vec<String>, Error> {
    let raw = req_str(scopes_json);
    serde_json::from_str::<Vec<String>>(&raw)
        .map_err(|e| Error::InvalidArgument(format!("invalid scopes JSON: {e}")))
}

/// スコープ集合を検証する (未知のスコープがあれば `err`)。
#[no_mangle]
pub unsafe extern "C" fn up_validate_scopes(scopes_json: *const c_char) -> *mut c_char {
    let scopes = match parse_scopes(scopes_json) {
        Ok(s) => s,
        Err(e) => return err_to_cstr(e),
    };
    match user_permission_core::validate_scopes(&scopes) {
        Ok(()) => ok_null(),
        Err(e) => err_to_cstr(e),
    }
}

/// サービスクライアントを作成する。`ok`: `{"client": ServiceClient, "secret": "..."}`。
/// secret は発行時にのみ取得できる (DB には Argon2 ハッシュのみ保存)。
#[no_mangle]
pub unsafe extern "C" fn up_service_clients_create(
    handle: *mut DbHandle,
    name: *const c_char,
    scopes_json: *const c_char,
    expires_at: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let scopes = match parse_scopes(scopes_json) {
        Ok(s) => s,
        Err(e) => return err_to_cstr(e),
    };
    let name = req_str(name);
    let expires_at = opt_str(expires_at);
    run_json(async move {
        db.service_clients()
            .create(&name, &scopes, expires_at.as_deref())
            .await
            .map(|(client, secret)| json!({ "client": client, "secret": secret }))
    })
}

/// 全サービスクライアントを取得する (`ok`: [ServiceClient])。
#[no_mangle]
pub unsafe extern "C" fn up_service_clients_list(handle: *mut DbHandle) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    run_json(async move { db.service_clients().list().await })
}

/// client_id でサービスクライアントを取得する (`ok`: ServiceClient | null)。
#[no_mangle]
pub unsafe extern "C" fn up_service_clients_get_by_client_id(
    handle: *mut DbHandle,
    client_id: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let client_id = req_str(client_id);
    run_json(async move { db.service_clients().get_by_client_id(&client_id).await })
}

/// サービスクライアントを削除する (`ok`: bool)。
#[no_mangle]
pub unsafe extern "C" fn up_service_clients_delete(
    handle: *mut DbHandle,
    id: i64,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    run_json(async move { db.service_clients().delete(id).await })
}

/// secret をローテートする (`ok`: string | null)。
#[no_mangle]
pub unsafe extern "C" fn up_service_clients_rotate_secret(
    handle: *mut DbHandle,
    id: i64,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    run_json(async move { db.service_clients().rotate_secret(id).await })
}
