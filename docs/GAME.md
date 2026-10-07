# Yggdrasil Authentication

This document provides instructions on how to set up and use a custom Yggdrasil authentication server for Minecraft. This allows you to authenticate users using your own server instead of the official Mojang servers.

## Table of Contents
- [Server](#server)
- [Client](#client)
- [Skins](#skins)
- [HTTPS Development certificate](#https-development-certificate)

## Server

1. Authlib-Injector is required to run the server. You can download it from [here](https://github.com/yushijinhun/authlib-injector).
2. Place the `authlib-injector.jar` file in the same directory as the server.jar / start script.
3. Example script to run the server:
```bash
#!/bin/bash

java -javaagent:authlib-injector.jar=https://localhost:36767/yggdrasil/ -Dauthlibinjector.disableHttpd -Dauthlibinjector.usernameCheck=enabled -Xms2048M -Xmx2048M -jar server.jar --nogui
```

4. Optionally, add the following JVM arguments to enable debug logging:
```bash
-Dauthlibinjector.debug
```

> **Legacy authentication must be enabled.** The auth-server endpoints used by authlib-injector
> (`/yggdrasil/authserver/{authenticate,refresh,validate,invalidate,signout}`) are gated behind
> `"Yggdrasil": { "EnableLegacyAuth": true }` in `appsettings.json` and return `403 Forbidden`
> while the flag is `false` (the default).
>
> The examples on this page use the Development environment (`https://localhost:36767`); the default
> port outside Development is `5001` (see [Getting Started](GETTING-STARTED.md#5-run)).

## Client
The client requires the following JVM arguments to be able to use the custom Yggdrasil server:
```bash
-Dminecraft.api.env=custom
-Dminecraft.api.auth.host=https://localhost:36767/yggdrasil/
-Dminecraft.api.account.host=https://localhost:36767/yggdrasil/
-Dminecraft.api.session.host=https://localhost:36767/yggdrasil/
-Dminecraft.api.services.host=https://localhost:36767/yggdrasil/
```

### Skins

To make custom skin server work on the client it must have the [MCCustomSkinLoader](https://github.com/xfl03/MCCustomSkinLoader) mod installed.
This is necessary because Mojang hardcoded their skin url checks on client side so a mod must modify that part of the code.
> Other mods may work as well, but this is the only one that has been tested and confirmed to work with the custom Yggdrasil server.
> If you make a custom client with MCP then the mod is optional as long as you know how to modify the URLs in the code of the client.

Example config
```json
{
  "version": "15.0.1",
  "buildNumber": 40,
  "loadlist": [
    {
      "name": "GameProfile",
      "type": "GameProfile"
    },
    {
      "name": "YggdrasilSharp",
      "type": "MojangAPI",
      "apiRoot": "https://localhost:36767/yggdrasil/",
      "sessionRoot": "https://localhost:36767/yggdrasil/"
    },
    {
      "name": "Mojang",
      "type": "MojangAPI",
      "apiRoot": "https://api.mojang.com/",
      "sessionRoot": "https://sessionserver.mojang.com/"
    }
  ],
  "enableTransparentSkin": false,
  "forceLoadAllTextures": true,
  "enableCape": true,
  "threadPoolSize": 8,
  "enableLogStdOut": false,
  "cacheExpiry": 30,
  "forceUpdateSkull": false,
  "enableLocalProfileCache": false,
  "enableCacheAutoClean": false,
  "forceDisableCache": false
}
```

## HTTPS Development certificate
Java is very strict about trusting certificates, so you will need to add the development certificate to your Java trust store.

First of all, the client uses custom java installation not the system one, so you will need to find the `cacerts` file in the `lib/security` folder of the custom java installation.
The following instructions will guide you through the process of generating a development certificate, getting its fingerprint, and adding it to the Java trust store.

> Please note that the commands provided in this section are for Linux and may need to be adjusted for other operating systems.

### Generating certificate

Usage of mkcert:
```bash
mkcert -install
mkcert localhost 127.0.0.1 ::1
```

Get the fingerprint of the generated certificate:
```bash
openssl x509 -in localhost+2.pem -noout -fingerprint -sha1
```

You should see the output like this, you need to strip the colons.
After that add set the value of the `CERTIFICATE_FINGERPRINT` variable in your .env file to the stripped fingerprint value.
```
Output:
SHA1 Fingerprint=AB:CD:EF:12:34:56:78:90:AB:CD:EF:12:34:56:78:90:AB:CD:EF

Final result:
ABCDEF1234567890ABCDEF1234567890ABCDEF
```

Creating a .pfx file from the generated certificate and private key:
```bash
openssl pkcs12 -export
  -out localhost.pfx
  -inkey localhost+2-key.pem
  -in localhost+2.pem
  -password pass:changeit
```

On Windows, copy the fingerprint of the generated certificate and modify your .env file to set the value of the `CERTIFICATE_FINGERPRINT` variable to the fingerprint of the generated certificate.
On Linux/MacO, copy the absolute path of the generated certificate and modify your .env file to set the value of the `CERTIFICATE_FINGERPRINT` variable to the path of the generated certificate.
Also modify the `CERTIFICATE_PASSWORD` variable to set the password of the generated certificate, in this case it is `changeit`.

Finally import it to the Java trust store.

```bash
cd <path_to_java_root>/bin
keytool -importcert
  -file <path_to_certificate>/localhost+2.pem
  -keystore ../lib/security/cacerts
  -alias client-dev
  -storepass changeit
```