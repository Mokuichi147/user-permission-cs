//! 同梱の axum HTTP サーバーを起動する FFI。
//!
//! `up_serve` は (正常時は) サーバーが停止するまでブロックする。呼び出し側 (.NET) は
//! `Task.Run` 上で呼ぶことを想定している。

use std::os::raw::c_char;
use std::path::PathBuf;
use std::time::Duration;

use user_permission::{build_app, WebConfig};
use user_permission_core::{Database, Error, Result};

use crate::{err_to_cstr, ok_null, req_str, runtime};

/// HTTP サーバーを起動する。`webui` は 0/1。失敗時のみ `err` を返す。
#[no_mangle]
pub unsafe extern "C" fn up_serve(
    database: *const c_char,
    secret: *const c_char,
    host: *const c_char,
    port: u16,
    prefix: *const c_char,
    webui: u8,
    webui_prefix: *const c_char,
) -> *mut c_char {
    let database = req_str(database);
    let secret = req_str(secret);
    let host = req_str(host);
    let prefix = req_str(prefix);
    let webui_prefix = req_str(webui_prefix);

    let result: Result<()> = runtime().block_on(async move {
        let db =
            Database::open_local(PathBuf::from(&database), Some(PathBuf::from(&secret))).await?;
        let config = WebConfig {
            api_prefix: prefix,
            webui_prefix,
            webui_enabled: webui != 0,
            token_expires: Duration::from_secs(3600),
            webui_token_expires: Duration::from_secs(86_400),
            ..WebConfig::default()
        };
        let app = build_app(db, config);
        let addr = format!("{host}:{port}");
        let listener = tokio::net::TcpListener::bind(&addr).await.map_err(Error::Io)?;
        axum::serve(listener, app).await.map_err(Error::Io)?;
        Ok(())
    });

    match result {
        Ok(()) => ok_null(),
        Err(e) => err_to_cstr(e),
    }
}
