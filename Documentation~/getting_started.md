# Getting Started

## Project Setup
Before using the deployment package make sure that your project is properly setup.

1. At the top Editor toolbar go to 
-  on `2021.3+`: `Edit > Project Settings... > Services`  
-  on `2022+`: `Services > General Settings > Services`
2. Create a Unity Project ID or link an existing one.
3. Switch to `Services > Deployment` tab and select the ["environment"](./environments.md) you want to work in.

## Locating Deployment Window
One of the main features of the deployment package is the ["Deployment Window"](./deployment_window.md). To open the window, at the top Editor toolbar go to 
- on `2021.3+`: `Window > Deployment`
- on `2022+`: `Services > Deployment`

## Making sure supported packages are installed
Deployment package does not provide functionality in isolation. Deployment Window will locate any `deployable assets` in your project. Deployable assets can be created only if a supported package is present in your project.
Make sure that one of the ["supported packages"](./index.md#supported-packages) are present.

## Creating Deployable Assets
Deployment package does not provide the ability to create deployable assets. For this, consult the documentation of a supported package you intent to use.

## Deploying Assets
Assuming you have installed a supported package and was able to create a deployable asset, you should be able to see them in the deployment window. There are multiple ways to deploy an asset.

1. Manual
- Double click on an item in the Deployment Window
- Right click on an item and choose `Deploy`
- Select an item or multiple items and hit `Deploy Selected`, which is located in the Deployment Window's toolbar.

2. On Play
- Click on the 3 dots in the right top corner of the Deployment Window and make sure `Deploy On Play` is toggled on.
- Enter play mode and see **all** of your items being deployed.

## Grouping Assets
To group assets you will need to utilize a ["Deployment Definition"](./deployment_definition.md). If no deployment definition is present, everything is grouped under a single `Default` deployment definition. 
Only a single deployment defintion can be created in a folder at a time, however, subfolders can contain other deployment definitions as well.

