

## D365 Automation Tools for Build & Release Pipelines

Use D365 Automation Tools to build deployment tasks for Dynamics 365 CE. The tool was developed based on the requirement that originated from actual projects.

### Extension Tasks
<style> table {font-size:14px;} #asterisk {color:red; font-weight:bold;} </style>
The following tasks are part of the automation tools:  **_( <span id="asterisk">*</span> -> Required )_**

 1. **Import Solution Using Configuration xml:** Import solution using a configuration xml file that has the list and sequence of solutions to be installed. The task requires the below input parameters for execution: 
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Configuration File<span id="asterisk">*</span>:_ Location of configuration file that details the solutions and sequence of installation for those. The configuration file is an XML file with the schema as below:
    `<deploymentartifact>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<solutions>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<solutionfile solutionpackagefilename="`**`<<Solution Relative Path>>`**`" overwriteunmanagedcustomizations="true" publishworkflowsandactivateplugins="true" importasholdingsolution="false" />`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`</solutions>`<br>
    `</deploymentartifact>`<br><br>
    
    |Node Name           |Attribute                          |Description                         |
    |--------------------|-----------------------------------|-----------------------------|
    |deploymentartifact  |                                   |_Root Node_            |
    |solutions           |                                   |_Parent Node enclosing all solutions to be imported_            |
    |solutionfile        |                                   |_Individual Node for solution file to be imported_|
    |solutionfile        |solutionpackagefilename            |_Relative Path of solution file name. eg <br>**".\SolutionTest_1_0_0_1_Managed.zip"**_|
    |solutionfile        |overwriteunmanagedcustomizations   |_in case of managed solutions, indicates if unmanaged customizations have to be overwritten_|
    |solutionfile        |publishworkflowsandactivateplugins |_Indicates if workflows and plugins that are part of the solution have to be activated_|
    |solutionfile        |importasholdingsolution            |_In case of managed solutions, indicates if solution upgrade has to be applied_|
    
    <br>

	-   _Enble Tracing:_ Display the trace logs during the task execution
---   
<a name="ImportCM"></a>

 2. **Import Configuration Migration Zip:** Import configuration migration zip files (generated from the D365 Configuration Migration Tool) using a configuration xml file that has the list and sequence of zip files to be imported. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Configuration File<span id="asterisk">*</span>:_ Location of configuration file that lists the zip files to be imported.
    -   _Replace Guids:_  Location of configuration file that has details about the guid to be replaced. This is required in case of association to records such as the root business unit, base currency id, etc which differs based on organization.
---    
 3. **Upsert Entity Record:** Insert or Update entity record based on the filter condition specified. Primarily used for updating configuration records. However this is not a bulk insert task and can only be used to insert or update a single record. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Entity Name<span id="asterisk">*</span>:_ Entity Logical Name
    -   _Retrieve Record By:_ Select if the record has to be retrieved by Record GUID or by specifying the filter condition.
    - _Record GUID **(Conditional)**:_ This is required only if the record is to be retrieved by Record ID. Record will be retrieved based on the specified GUID.
    - _Filter Condition (Fetch XML) **(Conditional)**:_ This is required if the search has to be performed based on the fetch xml. Only the filter node from the fetch xml has to be specified. eg:<br>
     `<filter type='and'>`<br>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`      <condition attribute='name' operator='eq' value='Administration' />`<br>
     `</filter>`<br><br>
     - _Attribute Name/Value JSON array<span id="asterisk">*</span>:_ Holds the attribute-value pair that has to be updated for the selected record. Follow the below format to specify the attribute values<br>
     `[{name:"accountid", value:"453234"},`<br>`{name:"accountname", value:"Contoso Industries"}]`<br>
     The attribute type is not required to be specified. However, in case of partylist attribute the lookup type is required to be specified. <br>
     `[{name:"accountid", value:"453234"},`<br>`{name:"regarding", value:"47cf9395-b066-43d6-b7cd-29f75df6e397", type:"account"}]`<br><br>
    - _Create Record (if not present):_ On selection, record will be created if there are no matching records 
---
4. **Enable Access Team:** Enable access team on D365 Entities. Use this task to enable it for single or multiple entities. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Entity List<span id="asterisk">*</span>:_ Entity Logical Name separated by comma. eg: <br>
     `account,contact,lead`
---
5. **Update Plugin Configuration:** Updates the secure or unsecure configuration for single or multiple plugin steps. The unsecure configurations are updated as part of the solution import. However, if there are environment specific values to be updated then this task can be utilized. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Plugin Configuration Type<span id="asterisk">*</span>:_ Indicate if the secure or unsecure configuration has to be updated
    -  _Configuration Value<span id="asterisk">*</span>:_ Name-value pair for plugin step name in the format below:<br>
     `{StepName1:Value1,StepName2:Value2,StepName3:Value3}`
    