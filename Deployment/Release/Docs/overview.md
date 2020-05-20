## D365 Automation Tools for Build & Release Pipelines
<style> table {font-size:14px;} #asterisk {color:red; font-weight:bold;} #notification {border:1px solid rgba(43,148,225,.25);background-color:rgba(43,148,225,.15);padding:6px 6px;} #note {font-weight:bold;color:red}</style>
Use D365 Automation Tools to build deployment tasks for Dynamics 365 CE. The tool was developed based on the requirement that originated from actual projects.

<div id="notification"><font id="note">NEW:</font> Added support to export and import Document Templates</div>

### Extension Tasks
The following tasks are part of the automation tools:  **_( <span id="asterisk">*</span> -> Required )_**

 1. **Import Solution Using Configuration xml:** Import solution using a configuration xml file that has the list and sequence of solutions to be installed. The task requires the below input parameters for execution: 
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Configuration File<span id="asterisk">*</span>:_ Location of configuration file that details the solutions and sequence of installation for those. The configuration file is an XML file with the schema as below:<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<deploymentartifact>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<solutions>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<solutionfile solutionpackagefilename="`**`<<Solution Relative Path>>`**`" overwriteunmanagedcustomizations="true" publishworkflowsandactivateplugins="true" importasholdingsolution="false" />`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`</solutions>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`</deploymentartifact>`<br><br>
    
    |Node Name           |Attribute                          |Description                         |
    |--------------------|-----------------------------------|-----------------------------|
    |deploymentartifact  |                                   |_Root Node_            |
    |solutions           |                                   |_Parent Node enclosing all solutions to be imported_            |
    |solutionfile        |                                   |_Individual Node for solution file to be imported_|
    |solutionfile        |solutionpackagefilename            |_Relative Path of solution file name. eg <br>**".\SolutionTest_1_0_0_1_Managed.zip"**_|
    |solutionfile        |overwriteunmanagedcustomizations   |_In case of managed solutions, indicates if unmanaged customizations have to be overwritten_|
    |solutionfile        |publishworkflowsandactivateplugins |_Indicates if workflows and plugins that are part of the solution have to be activated_|
    |solutionfile        |importasholdingsolution            |_In case of managed solutions, indicates if solution upgrade has to be applied_|
    
    <br>

	-   _Enble Tracing:_ Display the trace logs during the task execution
---   
<a name="ImportCM"></a>

 2. **Import Configuration Migration Zip:** Import configuration migration zip files (generated from the D365 Configuration Migration Tool) using a configuration xml file that has the list and sequence of zip files to be imported. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Configuration File<span id="asterisk">*</span>:_ Location of configuration file that lists the zip files to be imported. The configuration file is an XML file with the schema as below:<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<deploymentartifact>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<filestoimport>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<configimportfile filename="`**`<<MasterData Zip Path>>`**`" enablebatchmode="true" batchsize="500" concurrentthread="5" />`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`</filestoimport>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`</deploymentartifact>`<br><br>
    
    |Node Name           |Attribute                          |Description                         |
    |--------------------|-----------------------------------|-----------------------------|
    |deploymentartifact  |                                   |_Root Node_            |
    |filestoimport       |                                   |_Parent Node enclosing all zip files to be imported_            |
    |configimportfile    |                                   |_Individual Node for zip file to be imported_|
    |configimportfile    |filename                           |_Relative Path of MasterData zip file name. eg <br>**".\MasterData\BU.zip"**_|
    |configimportfile    |enablebatchmode                    |_(Optional) Enable batch mode for the file import_|
    |configimportfile    |batchsize                          |_(Conditional) Required if the batch mode is enabled_|
    |configimportfile    |concurrentthread                   |_Number of concurrent threads for the data import_|
    
    <br>

    -   _Replace Guids:_  Location of configuration file that has details about the guid to be replaced. This is required in case of association to records such as the root business unit, base currency id, etc which differs based on organization. This file should be of type ".json" and should have the format as below. Please note that the below GUID is from the source environment (i.e the environment from which the data zip is extracted). The task will replace the below GUID with the value from the target environment.<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`[{`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"entity": "businessunit",`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"guid": "8165f4a8-5730-4e2b-b120-6b4e71bfd0d7",`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"filter": "<condition attribute='parentbusinessunitid' operator='null' />"`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`},`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`{`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"entity": "transactioncurrency",`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"guid": "f0e12181-1c4d-4d62-879f-17b1ce391b4f",`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"filter": "<condition attribute='isocurrencycode' operator='eq' value='USD' />"`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`}]`
---   
<a name="UpsertEntity"></a>

 3. **Upsert Entity Record:** Insert or Update entity record based on the filter condition specified. Primarily used for updating configuration records. However this is not a bulk insert task and can only be used to insert or update a single record. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Entity Name<span id="asterisk">*</span>:_ Entity Logical Name
    -   _Retrieve Record By:_ Select if the record has to be retrieved by Record GUID or by specifying the filter condition.
    - _Record GUID **(Conditional)**:_ This is required only if the record is to be retrieved by Record ID. Record will be retrieved based on the specified GUID.
    - _Filter Condition (Fetch XML) **(Conditional)**:_ This is required if the search has to be performed based on the fetch xml. Only the filter node from the fetch xml has to be specified. eg:<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`<filter type='and'>`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`      <condition attribute='name' operator='eq' value='Administration' />`<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`</filter>`<br><br>
     - _Attribute Name/Value JSON array<span id="asterisk">*</span>:_ Holds the attribute-value pair that has to be updated for the selected record. Follow the below format to specify the attribute values<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`[{`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`name:"accountid",`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`value:"453234"`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`},{`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`name:"accountname",`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`value:"Contoso Industries"`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`}]`<br>
     The attribute type is not required to be specified. However, in case of partylist attribute the lookup type is required to be specified. <br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`[{`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`name:"accountid",`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`value:"453234"`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`},{`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`name:"regarding",`<br> 
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`value:"47cf9395-b066-43d6-b7cd-29f75df6e397",`<br> 
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`type:"account"`<br>
	&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`}]`<br><br>
    - _Create Record (if not present):_ On selection, record will be created if there are no matching records 
---
<a name="EnableAccessTeam"></a>

4. **Enable Access Team:** Enable access team on D365 Entities. Use this task to enable it for single or multiple entities. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Entity List<span id="asterisk">*</span>:_ Entity Logical Name separated by comma. eg: <br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`account,contact,lead`
---
<a name="UpdatePluginConfiguration"></a>

5. **Update Plugin Configuration:** Updates the secure or unsecure configuration for single or multiple plugin steps. The unsecure configurations are updated as part of the solution import. However, if there are environment specific values to be updated then this task can be utilized. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Plugin Configuration Type<span id="asterisk">*</span>:_ Indicate if the secure or unsecure configuration has to be updated
    -  _Configuration Value<span id="asterisk">*</span>:_ Name-value pair for plugin step name in the format below:<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`{StepName1:Value1,StepName2:Value2,StepName3:Value3}`
 ---
<a name="ExportAccessTeamTemplate"></a>

6. **Export  Access Team Templates:** Export the configured Access Team Templates from the source environment.  The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Output File<span id="asterisk">*</span>:_ The absolute path for the file that is required to hold the Access Team details. The file should be of type "json" and will be created if the file does not exist.
 ---
<a name="ImportAccessTeamTemplate"></a>

7. **Import  Access Team Templates:** Create or Update the Access Team Templates. Details about the Access Team Templates are read from a configuration file. The task '[Export Access Team Template](#ExportAccessTeamTemplate)' can be used to generate the configuration file. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Configuration File<span id="asterisk">*</span>:_ Configuration file generated from '[Export Access Team Template](#ExportAccessTeamTemplate)' 
 ---
<a name="ExportDocumentTemplate"></a>

8. **Export Document Templates:** Export the configured Document Templates from the source environment.  The task requires the below input parameters for execution:
    -  _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -  _Retrieve Templates<span id="asterisk">*</span>:_ Indicates if all the templates or selected templates have to be exported from the source environment. 
    -  _Template Names  **(Conditional)**:_  This is required only if the template is to be retrieved based on the template names. The names have to be specified in the format below:<br>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`["Template 1","Template 2","Template 3"]`    
    -   _Output Folder Location<span id="asterisk">*</span>:_ The absolute path of the folder that is required to hold the exported templates. In addition to the exported templates, a configuration file with extension of type "json" is created. This configuration file is later used to import the templates to the target environments. 
 ---
<a name="ImportDocumentTemplate"></a>

9. **Import Document Templates:** Create or Update the Document Templates. Details about the document templates are read from a configuration file. The task '[Export Document Templates](#ExportDocumentTemplate)' can be used to generate the configuration file. The task requires the below input parameters for execution:
    -   _Connection String<span id="asterisk">*</span>:_ Connection string to connect to the D365 Instance. Please refer 
    [here](https://docs.microsoft.com/en-us/powerapps/developer/common-data-service/xrm-tooling/use-connection-strings-xrm-tooling-connect#connection-string-parameters) for further details
    -   _Configuration File<span id="asterisk">*</span>:_ Configuration file generated from '[Export Document Templates](#ExportDocumentTemplate)' 
   

## Release Notes

05/17/2020:

 - Added Support for Export and Import of Document Templates

05/08/2020:

 - Initial Release version
