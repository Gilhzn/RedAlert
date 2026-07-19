# פריסה והפצה / Deployment

## בניית המשחק (במחשב עם Unity)
תפריט **Tiberium Dusk → Build**:
- **WebGL** → `builds/webgl` — נבנה עם Gzip + decompression fallback (עובד על כל שרת סטטי)
- **Windows / Linux** — קבצי הפעלה לדסקטופ

לפני כל בנייה, `data/` מועתק אוטומטית ל-StreamingAssets (ה-hook רץ לבד; אפשר גם ידנית מהתפריט).

## אירוח ה-WebGL
כל אחסון סטטי עובד: itch.io (העלה כ-zip עם "This file will be played in the browser"), GitHub Pages, Netlify, S3+CloudFront, או nginx.
אם משביתים את ה-decompression fallback לביצועים, נדרשות כותרות `Content-Encoding: gzip` לקבצי `.gz`.

## שרת המולטיפלייר
```bash
dotnet publish server/TiberiumDusk.Server -c Release -o publish
./publish/TiberiumDusk.Server 7777 2     # פורט, מספר שחקנים להתחלה
```
systemd (לשרת לינוקס):
```ini
[Unit]
Description=Tiberium Dusk relay
[Service]
ExecStart=/opt/tiberiumdusk/TiberiumDusk.Server 7777 2
Restart=always
[Install]
WantedBy=multi-user.target
```

### חשוב ל-WebGL: ‏wss://
דפדפן בעמוד https יתחבר רק ל-`wss://` (TLS). הצב את ה-relay מאחורי reverse proxy:
```nginx
location /game/ {
    proxy_pass http://127.0.0.1:7777/;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
}
```
ובכתובת בתפריט: `wss://your-domain.com/game/`.

## מגבלות ידועות
- מולטיפלייר WebGL עדיין לא נבדק מקצה־לקצה (ה-transport קיים; דסקטופ/Editor מאומת בבדיקות)
- אין reconnect אחרי ניתוק; אין לובי רב־חדרים (שרת = חדר)
