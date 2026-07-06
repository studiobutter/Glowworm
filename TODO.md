# TODO - Emergency Bug Fix

HoYo have recently made a minor URL Change which broke Genshin Gacha Log System (hk4e): 
- The original system checks for a matching URL: https://gs.hoyoverse.com/genshin/event/e20190909gacha-v3/index.html. HoYo change the v3 to df01aea which broke the Gacha Import system. 
- To fix this, instead of matching the full URL, we will only match the following: https://gs.hoyoverse.com/genshin/event/e20190909gacha. This help so incase any changes was made to the version, it won't break the system.
- This also applies to both Global and Chinese servers, and all HoYo games, removing the version and only rely part of the URL to find a match. (hk4e, hkrpg, nap)

Additional fix for Zenless Gacha: I also realized that we use the Link: https://public-operation-common-sg.hoyoverse.com/common/gacha_record/api/getGachaLog for getting ZZZ Gacha Log. While it works, it limits the amount of items we can import since the size is fixed at 5. Which is not the case with Genshin and HSR Gacha URLs.

Additionally, the game specific URL: https://public-operation-nap-sg.hoyoverse.com/common/gacha_record/api/getGachaLog does work to get the same data but can change how many data we can import. (i.e: 10, 20, etc)

To help with that, we will need to implement a better importing system. While the app can still detects public-operation-common link, instead of pulling from the link, we only need to know the following:

Zenless (nap_global/nap_cn)
- Fixed data:
  - authkey_ver=1
  - sign_type=2
  - auth_appid=webview_gacha
- Dynamic Data
  - authkey= (Can only obtain from URL)
  - lang=en (system language or user set language)
  - region= (Can only obtain from URL)
  - game_biz=nap_global/nap_cn
  - page= 
  - size=5 (Bigger the number, the more data can be imported)
  - real_gacha_type=3 (imported from a set of gacha types)
  - end_id=

Genshin (hk4e): 
- Fixed data:
  - authkey_ver=1
  - sign_type=2
  - auth_appid=webview_gacha
- Dynamic Data
  - authkey= (Can only obtain from URL)
  - lang=en (system language or user set language)
  - region= (Can only obtain from URL)
  - game_biz=hk4e_global/hk4e_cn
  - page= 
  - size=5 (Bigger the number, the more data can be imported)
  - gacha_type=
  - end_id=

Genshin - Miliastra Wonderland (hk4e_ugc)
- Fixed data:
  - authkey_ver=1
  - sign_type=2
  - auth_appid=webview_gacha
- Dynamic Data
  - authkey= (Can only obtain from URL)
  - lang=en (system language or user set language)
  - region= (Can only obtain from URL)
  - game_biz=hk4e_global/hk4e_cn
  - page= 
  - size=5 (Bigger the number, the more data can be imported)
  - gacha_type=
  - end_id=

Star Rail: 
- Fixed data:
  - authkey_ver=1
  - sign_type=2
  - auth_appid=webview_gacha
- Dynamic Data
  - authkey= (Can only obtain from URL)
  - lang=en (system language or user set language)
  - region= (Can only obtain from URL)
  - game_biz=hkrpg_global/hkrpg_cn
  - page= 
  - size=5 (Bigger the number, the more data can be imported)
  - gacha_type= (imported from a set of gacha types)
  - end_id=

Once obtained the URL Query Parameters, use the same parameters to get the user's Gacha Data using the game specifc URL. 

For Input URL, accept the following URLs:
Genshin: 
  - Global: https://public-operation-hk4e-sg.hoyoverse.com/gacha_info/api/getGachaLog, https://gs.hoyoverse.com/genshin/event/e20190909gacha (similar URL)
  - China: https://public-operation-hk4e.mihoyo.com/gacha_info/api/getGachaLog, https://webstatic.mihoyo.com/hk4e/event/e20190909gacha (similar URL)
Genshin - Miliastra: 
  - Global: https://public-operation-hk4e-sg.hoyoverse.com/gacha_info/api/getBeyondGachaLog, https://gs.hoyoverse.com/genshin/event/e20250716gacha (similar URL)
  - China: https://public-operation-hk4e.mihoyo.com/gacha_info/api/getBeyondGachaLog, https://webstatic.mihoyo.com/hk4e/event/e20250716gacha (similar URL)
Star Rail: 
  - Global: https://public-operation-hkrpg-sg.hoyoverse.com/common/gacha_record/api/getGachaLog, https://public-operation-hkrpg-sg.hoyoverse.com/common/hkrpg_gacha_record/api/getGachaLog, https://gs.hoyoverse.com/hkrpg/event/e20211215gacha (similar URL)
  - China: https://public-operation-hkrpg.mihoyo.com/common/gacha_record/api/getGachaLog, https://public-operation-hkrpg.mihoyo.com/common/hkrpg_gacha_record/api/getGachaLog, https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha (similar URL)
ZZZ: 
  - Global: https://public-operation-common-sg.hoyoverse.com/common/gacha_record/api/getGachaLog, https://public-operation-nap-sg.hoyoverse.com/common/gacha_record/api/getGachaLog, https://gs.hoyoverse.com/nap/event/e20230424gacha (similar URL)
  - China: ttps://public-operation-common.mihoyo.com/common/gacha_record/api/getGachaLog, https://public-operation-nap.mihoyo.com/common/gacha_record/api/getGachaLog, https://gs.hoyoverse.com/nap/event/e20230424gacha (similar URL)