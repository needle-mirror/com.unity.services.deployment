# Deployment Window
This window allows you to upload cloud assets to their respective cloud service.
To access the deployment window:
* In 2021 or before, use `Window > Deployment`
* In 2022 and later, use `Services > Deployment`
 
## Toolbar
The toolbar in the Deployment Window contains a set of features to help with deploying assets.

### Deploy Selected
This button will deploy the selected assets to the chosen environment.

### Deploy All
This button will deploy all assets to the chosen environment.

### Environment View
This dropdown shows the current environment unto which
the assets will be deployed to.

This dropdown contains a shortcut to 
`Project Settings > Services > Deployment > Environment Selector` where the
environment might be modified.

### Additional Options (⋮) 
This toolbar menu contains additional options of the
Deployment Window.

#### Deploy All on Play
This toggle triggers all assets to be deployed when the Unity Editor transitions from edit mode to play mode.
The Unity Editor will wait for file deployment to be completed before entering play mode.

## Asset List
Lists assets that can be deployed in the Deployment Window.
The assets can be uniquely or multi-selected.

### Context Menu
A custom context menu can be accessed when right-clicking on an asset within the Asset List. 

A few options are available, but more might be available depending on the service.

#### Deploy
Deploys the selected items

#### Select In Project Window
Selects the asset in the Project Window if it is an Unity asset

#### Open 
Opens the asset in the appropriate editor.
This might not be available to all items.

### Asset Status
Describes the status of an asset.

### Warning
The warning icon appears to bring attention to an issue with the asset

### Error
The error icon will appear when the asset has failed to deploy 
or other failure states associated with each service.
