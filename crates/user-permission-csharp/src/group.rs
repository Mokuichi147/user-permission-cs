//! グループ管理 FFI (`db.groups()` 相当)。

use std::os::raw::c_char;

use user_permission_core::GroupUpdate;

use crate::{db_of, err_to_cstr, opt_str, req_str, run_json, tri_bool, user_id_of, DbHandle};

/// グループを作成する (`ok`: Group)。
#[no_mangle]
pub unsafe extern "C" fn up_groups_create(
    handle: *mut DbHandle,
    name: *const c_char,
    description: *const c_char,
    is_admin: u8,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let name = req_str(name);
    let description = req_str(description);
    let token = opt_str(token);
    run_json(async move {
        db.groups()
            .create(&name, &description, is_admin != 0, token.as_deref())
            .await
    })
}

/// ID でグループを取得する (`ok`: Group | null)。
#[no_mangle]
pub unsafe extern "C" fn up_groups_get_by_id(
    handle: *mut DbHandle,
    group_id: i64,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.groups().get_by_id(group_id, token.as_deref()).await })
}

/// 名前でグループを取得する (`ok`: Group | null)。
#[no_mangle]
pub unsafe extern "C" fn up_groups_get_by_name(
    handle: *mut DbHandle,
    name: *const c_char,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let name = req_str(name);
    let token = opt_str(token);
    run_json(async move { db.groups().get_by_name(&name, token.as_deref()).await })
}

/// 全グループを取得する (`ok`: [Group])。
#[no_mangle]
pub unsafe extern "C" fn up_groups_list_all(
    handle: *mut DbHandle,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.groups().list_all(token.as_deref()).await })
}

/// 管理者グループのみ取得する (`ok`: [Group])。
#[no_mangle]
pub unsafe extern "C" fn up_groups_list_admin_groups(
    handle: *mut DbHandle,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.groups().list_admin_groups(token.as_deref()).await })
}

/// グループを更新する。文字列引数は null で「変更なし」。`is_admin` は -1=変更なし / 0=false / 1=true。
#[no_mangle]
pub unsafe extern "C" fn up_groups_update(
    handle: *mut DbHandle,
    group_id: i64,
    name: *const c_char,
    description: *const c_char,
    is_admin: i32,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let update = GroupUpdate {
        name: opt_str(name),
        description: opt_str(description),
        is_admin: tri_bool(is_admin),
    };
    let token = opt_str(token);
    run_json(async move {
        db.groups()
            .update(group_id, update, token.as_deref())
            .await
    })
}

/// グループを削除する (`ok`: bool)。
#[no_mangle]
pub unsafe extern "C" fn up_groups_delete(
    handle: *mut DbHandle,
    group_id: i64,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move { db.groups().delete(group_id, token.as_deref()).await })
}

/// グループにユーザーを追加する (`ok`: bool)。
#[no_mangle]
pub unsafe extern "C" fn up_groups_add_user(
    handle: *mut DbHandle,
    group_id: i64,
    user_id: *const c_char,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let user_id = match user_id_of(user_id) {
        Ok(id) => id,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move {
        db.groups()
            .add_user(group_id, user_id, token.as_deref())
            .await
    })
}

/// グループからユーザーを除外する (`ok`: bool)。
#[no_mangle]
pub unsafe extern "C" fn up_groups_remove_user(
    handle: *mut DbHandle,
    group_id: i64,
    user_id: *const c_char,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let user_id = match user_id_of(user_id) {
        Ok(id) => id,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move {
        db.groups()
            .remove_user(group_id, user_id, token.as_deref())
            .await
    })
}

/// グループのメンバー一覧を取得する (`ok`: [User])。
#[no_mangle]
pub unsafe extern "C" fn up_groups_get_members(
    handle: *mut DbHandle,
    group_id: i64,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move {
        db.groups()
            .get_members(group_id, token.as_deref())
            .await
    })
}

/// ユーザーが所属するグループ一覧を取得する (`ok`: [Group])。
#[no_mangle]
pub unsafe extern "C" fn up_groups_get_user_groups(
    handle: *mut DbHandle,
    user_id: *const c_char,
    token: *const c_char,
) -> *mut c_char {
    let db = match db_of(handle) {
        Ok(d) => d,
        Err(e) => return err_to_cstr(e),
    };
    let user_id = match user_id_of(user_id) {
        Ok(id) => id,
        Err(e) => return err_to_cstr(e),
    };
    let token = opt_str(token);
    run_json(async move {
        db.groups()
            .get_user_groups(user_id, token.as_deref())
            .await
    })
}
