# Feature: auth


## Requirements Tree

- **[REQ]** 商品購入機能を提供すること
  - *[RAT]* 在庫の引き当てを確実に行い、商品の過剰販売（売り越し）を防ぐため
    - **[SPEC]** 購入ボタン押下時、在庫サーバは在庫を減算する
      > **EARS:** `EventDriven` | **When:** 購入ボタン押下 | **Actor:** 在庫サーバ | **Response:** 在庫を減算する
    - **[SPEC]** 決済タイムアウト時、在庫サーバは処理を中断する
      > **EARS:** `EventDriven` | **When:** 決済タイムアウト時 | **Actor:** 在庫サーバ | **Response:** 処理を中断する
