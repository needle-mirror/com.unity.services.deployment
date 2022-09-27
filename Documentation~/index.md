# Deployment Package

This package contains the [Deployment Window](./deployment_window.md)
implementation and its associated features.

See ["Getting Started"](./getting_started.md) to get started.
See ["Package Usage"](./package_usage.md) for details.

## Deployment Window

The [Deployment Window](./deployment_window.md) allows to deploy
assets to their respective services, as well as other relevant
service specific manipulations.

It is a cohesive interface for all service asset deployment needs.

> A "deployment" is when service configuration files are put in sync
with service resources so that service clients can interact with them.

## Deployment Definition

A deployment definition is the logical corollary of assembly
definition but for service assets.

It allows you to group them and have additional settings associated with them.

For more information, consult the [Deployment Definition](./deployment_definition.md) documentation.

## Environments

The package contains a project setting to allow changing
the target environment for deployment purposes.

For more information, consult the [Environments](./environments.md) documentation.

## [Supported Packages](#supported-packages)

[com.unity.services.cloudcode](https://docs.unity.com/cloud-code) **from 2.1.0**
[com.unity.remote-config](https://docs.unity.com/remote-config) **from 3.2.0**