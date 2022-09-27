# Environments
Environments are logical partitions for Unity Game Services that contain data associated with your project. 
Examples can include game code using Cloud Code, or game configurations using Remote Config.

All users are provided with a default environment, the `production` environment.

For more in depth docs for environments, click [here](https://docs.unity.com/ugs-overview/unity-environments.htm).

## Creating an Environment
In order to create a new environment, you will need to visit the [unity dashboard](https://dashboard.unity3d.com).
Navigate to `Projects` on the sidebar, select a project in the dropdown at the top, and then click `Environments`.

This page will allow you to create and delete environments. Please note that the default environment cannot be deleted.

## Selecting an Environment
With this package installed, navigate to `Project Settings > Services > Deployment`. 
This window will allow you to select which environment you would like to use for deploying Cloud Code scripts. 
