# Générer un APK Android avec .NET 10 MAUI

Ce guide explique comment générer un package d'installation Android (`.apk`) pour une application développée avec .NET 10 MAUI.


## 1. Configurer le projet

Dans Visual Studio, passer la configuration en **Release** et sélectionner **Android** comme cible de démarrage.

**Attention :** au 15/10/2026, en raison d'un problème lié au framework Mono, le lancement du projet en mode `Release` peut échouer, même si la compilation ne présente aucune erreur en mode `Debug`.


## 2. Vérifier le fichier `.csproj`

Vérifier que le fichier `.csproj` contient un groupe de propriétés similaire à celui-ci :

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $(TargetFramework.Contains('-android')) == true">
    <!-- Package -->
    <AndroidPackageFormats>apk</AndroidPackageFormats>
    <AndroidGenerateAppBundle>true</AndroidGenerateAppBundle>

    <!-- Build -->
    <AndroidUseAapt2>True</AndroidUseAapt2>
    <JavaMaximumHeapSize>3G</JavaMaximumHeapSize>

    <!-- Linking & Trimming -->
    <PublishTrimmed>True</PublishTrimmed>
    <AndroidLinkMode>SdkOnly</AndroidLinkMode>

    <!-- AOT Settings -->
    <RunAOTCompilation>True</RunAOTCompilation>
    <AndroidEnableProfiledAot>False</AndroidEnableProfiledAot>
    <EnableLLVM>True</EnableLLVM>
    <AndroidStripILAfterAOT>True</AndroidStripILAfterAOT>
    <MauiStripXaml>True</MauiStripXaml>

    <!-- MultiDex -->
    <AndroidEnableMultiDex>True</AndroidEnableMultiDex>

    <!-- Target Runtimes -->
    <RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>
</PropertyGroup>
```


## 3. Générer le package

Ouvrir un terminal et se placer dans le répertoire contenant la solution `.sln` ou `.slnx`.

Exécuter la commande suivante en remplaçant `NomProjet.csproj` par le nom du fichier du projet :

```powershell
dotnet publish .\NomProjet.csproj -f net10.0-android -c Release -p:RuntimeIdentifier=android-arm64
```

Attendre la fin de la publication.


## 4. Récupérer le fichier APK

Les fichiers générés se trouvent dans le répertoire suivant :

```text
NomProjet\bin\Release\net10.0-android\android-arm64\publish\
```

Deux fichiers `.apk` peuvent être présents. Le fichier APK non signé peut être refusé par Android lors de l'installation.

**Utiliser le fichier dont le nom se termine par `-Signed.apk`** pour installer l'application sur l'appareil.


## 5. Installer l'application sur un appareil Android

1. Transférer le fichier `-Signed.apk` sur l'appareil Android, par câble USB, par e-mail ou par tout autre moyen.
2. Ouvrir le fichier depuis l'explorateur de fichiers de l'appareil.
3. Si nécessaire, autoriser l'installation d'applications provenant de cette source.
4. Si Android affiche un avertissement, consulter les détails et confirmer l'installation uniquement si l'application est fiable.

L'application est ensuite installée et peut être utilisée.

**Remarque :** les intitulés des menus et les avertissements peuvent varier selon la version d'Android et le fabricant de l'appareil.