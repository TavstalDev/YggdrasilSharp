# Yggdrasil Authentication

This document provides instructions on how to set up and use a custom Yggdrasil authentication server for Minecraft. This allows you to authenticate users using your own server instead of the official Mojang servers.

## Table of Contents
- [Server](#server)
- [Client](#client)
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

## Client
The client requires the following JVM arguments to be able to use the custom Yggdrasil server:
```bash
-Dminecraft.api.env=custom
-Dminecraft.api.auth.host=https://localhost:36767/yggdrasil/
-Dminecraft.api.account.host=https://localhost:36767/yggdrasil/
-Dminecraft.api.session.host=https://localhost:36767/yggdrasil/
-Dminecraft.api.services.host=https://localhost:36767/yggdrasil/
```

Optionally for HD skin support, it needs the CustomSkinLoader mod installed, you can download it from [here](https://modrinth.com/mod/customskinloader).
> Other mods may work as well, but this is the only one that has been tested and confirmed to work with the custom Yggdrasil server.

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

Finally import it to the launcher Java trust store.
The java installation is located in the `<launcher_root>/java/jdk-21.0.8+9` directory.
In debug mode the launcher will create its directory (`LauncherDebug`) to the same directory where the executable is located, the path of the executable is `<path_to_project>/MMC-Launcher/bind/Debug/net9.0/<arch>/MMC-Launcher`.

```bash
cd <launcher_root>/java/jdk-21.0.8+9/bin
keytool -importcert
  -file <path_to_certificate>/localhost+2.pem
  -keystore ../lib/security/cacerts
  -alias mmc-dev
  -storepass changeit
```