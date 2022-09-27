# Deployment Definitions
Deployment Definitions are analogous to assembly definitions,
but associated with deployable assets.

They allow you to group a set of deployable assets or code
into a logical grouping.

This grouping can be used for easier deployment later
in the Deployment Window.

By default, all assets will be assigned to a `Default`
Deployment Definition.

## Creating a Deployment Definition

A Deployment Definition can be created in any folder by right
clicking on the Project Window it then clicking `Create > Deployment Defintion`.

There can only be one Deployment Definition per folder, similar
to assembly definitions. 

All deployable assets inside that folder and sub-folders will be
assigned to that Deployment Definition.

A Deployment Definition can be modified in the `Inspector` by selecting
it from the `Project Window`. 

Additionally, it can be located in the `Project Window` by right clicking
on it in the [Deployment Window](./deployment_window.md) 
and choosing `Select in Project Window` in the context menu.

You can also chose to open it in your preferred IDE from the `Project Window`.

## Deploying a Deployment Definition

Deployment Definitions will show up in the [Deployment Window](./deployment_window.md) with all
associated assets under its hierarchy.

A Deployment Definition can be deployed by selecting and clicking Deploy,
or by right-clicking and selecting the `Deploy` context menu item.
