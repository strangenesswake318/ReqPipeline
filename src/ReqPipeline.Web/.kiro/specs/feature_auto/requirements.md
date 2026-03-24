# Feature: feature_auto

## Requirements Tree

- **[EPIC]** システムは一般ユーザーに対して、正当なアカウント所有者であることを証明するための本人確認（ログイン）機能を提供し、認証に成功したユーザーのみが自身のデータにアクセスできる状態を保証すること。
  - *[WHY]* 近年、なりすましによる不正アクセスが多発しているため。一般ユーザーが「自分のデータや資産が第三者に盗まれるかもしれない」という不安を抱くことなく、絶対的な安心感をもって当社のオンラインサービスを継続利用できるようにするため。
    - **[SCENARIO]** ログイン失敗時のアカウントロック処理
      > **BDD:** Given `[SYS:User_Unauthenticated]` | When `[SW:Auth:Login_Failed] が連続で発生する` | Then `アカウントをロックする`
      - **[RULE]** ログイン失敗時、認証サーバはアカウントをロックする
        > **EARS:** `[EventDriven]` | Trigger: `[SW:Auth:Login_Failed]` | Actor: `認証サーバ` | Response: `アカウントをロックする`
      - **[RULE]** ログイン失敗時、認証サーバは適切に入力ミスを無視する。
        > **EARS:** `[EventDriven]` | Trigger: `ログイン失敗時` | Actor: `認証サーバ` | Response: `適切に入力ミスを無視する`
