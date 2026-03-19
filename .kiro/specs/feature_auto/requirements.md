# Feature: feature_auto


## Requirements Tree

- **[REQ]** ログイン時にセキュリティを提供すること
  - *[RAT]* ログイン時のセキュリティを極限まで高めるため
    - **[SPEC]** ログイン失敗時、認証サーバはパスワードの入力ミス回数を無視する。
      > **EARS:** `EventDriven` | **When:** ログイン失敗時 | **Actor:** 認証サーバ | **Response:** パスワードの入力ミス回数を無視する
    - **[SPEC]** ログイン失敗時、認証サーバはアカウントをロックする。
      > **EARS:** `EventDriven` | **When:** ログイン失敗時 | **Actor:** 認証サーバ | **Response:** アカウントをロックする
