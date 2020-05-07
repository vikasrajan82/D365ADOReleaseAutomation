## D365 Automation Tools for Build & Release Pipelines**

Use D365 Automation Tools to build deployment tasks for Dynamics 365 CE. The tool was developed based on the requirement that originated from actual projects.

### ## Extension Tasks

The following tasks are part of the automation tools:

 3. **Upsert Entity Record:** Import configuration migration zip files (generated from the D365 Configuration Migration Tool) using a
    configuration xml file that has the list and sequence of zip files
    to be imported. The task requires the below input parameters for
    execution:

    -   _Connection String*:_ Connection string to connect to the D365 Instance. Please refer [test1](test1.htm) for further details
    -   _Configuration File*:_ Location of configuration file that lists the zip files to be imported.
    -   _Replace Guids:_  Location of configuration file that has details about the guid to be replaced. This is required in case of association to records such as the root business unit, base currency id, etc which differs based on organization.
