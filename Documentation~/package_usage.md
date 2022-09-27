# Package Usage

## Overview
The Deployment package allows for a cohesive interface for the
Deployment of assets for all UGS services.

## Intended Usage
The Deployment package is not used on its own.

Services will integrate with it, allowing for all of the game service configuration to
be saved in version-control with all of the advantages associated with it.

The Deployment package will provide the base tooling so that all of the services
have shared functionality surrounding the deployment of the service configuration files
on the servers themselves.

A "deployment" is when service configuration files are put in sync 
with service resources so that service clients can interact with them.


## Deployment Definitions
Deployment Definitions allow for grouping of the deployable assets.
To create a Deployment Definition click on `Create -> Deployment Definition`.

For more information on Deployment Definitions, please view its [documentation](./deployment_definition.md).

## File Deployment
To deploy a file, the [Deployment Window](./deployment_window.md) must be open. There are multiple ways to deploy files:

1. Click on `Deploy All` this would deploy all of the files displayed in the deployment window.
2. Double click on a file you wish to deploy.
3. Double click on a Deployment Definition to deploy all of the items under it.
4. Select multiple files and hit `Deploy Selected`.
5. If `Deploy On Play` toggle is on, all of the deployable files will be deployed on `Play`.

