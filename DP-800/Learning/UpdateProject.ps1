# Run this script after closing the Visual Studio solution
# It updates Learning.sqlproj to include all files properly

$projectFile = "Learning.sqlproj"

$newContent = @'
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" ToolsVersion="4.0">
  <PropertyGroup>
	<Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
	<Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
	<Name>Learning</Name>
	<SchemaVersion>2.0</SchemaVersion>
	<ProjectVersion>4.1</ProjectVersion>
	<ProjectGuid>{3fa1c57c-7e28-4109-8bc3-d5fc3bdd61d4}</ProjectGuid>
	<DSP>Microsoft.Data.Tools.Schema.Sql.Sql170DatabaseSchemaProvider</DSP>
	<OutputType>Database</OutputType>
	<RootPath>
	</RootPath>
	<RootNamespace>Learning</RootNamespace>
	<AssemblyName>Learning</AssemblyName>
	<ModelCollation>1033,CI</ModelCollation>
	<DefaultFileStructure>BySchemaAndSchemaType</DefaultFileStructure>
	<DeployToDatabase>True</DeployToDatabase>
	<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
	<TargetLanguage>CS</TargetLanguage>
	<AppDesignerFolder>Properties</AppDesignerFolder>
	<SqlServerVerification>False</SqlServerVerification>
	<IncludeCompositeObjects>True</IncludeCompositeObjects>
	<TargetDatabaseSet>True</TargetDatabaseSet>
	<DefaultCollation>SQL_Latin1_General_CP1_CI_AS</DefaultCollation>
	<DefaultFilegroup>PRIMARY</DefaultFilegroup>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
	<OutputPath>bin\Release\</OutputPath>
	<BuildScriptName>$(MSBuildProjectName).sql</BuildScriptName>
	<TreatWarningsAsErrors>False</TreatWarningsAsErrors>
	<DebugType>pdbonly</DebugType>
	<Optimize>true</Optimize>
	<DefineDebug>false</DefineDebug>
	<DefineTrace>true</DefineTrace>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
	<OutputPath>bin\Debug\</OutputPath>
	<BuildScriptName>$(MSBuildProjectName).sql</BuildScriptName>
	<TreatWarningsAsErrors>false</TreatWarningsAsErrors>
	<DebugSymbols>true</DebugSymbols>
	<DebugType>full</DebugType>
	<Optimize>false</Optimize>
	<DefineDebug>true</DefineDebug>
	<DefineTrace>true</DefineTrace>
	<ErrorReport>prompt</ErrorReport>
	<WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup>
	<VisualStudioVersion Condition="'$(VisualStudioVersion)' == ''">11.0</VisualStudioVersion>
	<SSDTExists Condition="Exists('$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v$(VisualStudioVersion)\SSDT\Microsoft.Data.Tools.Schema.SqlTasks.targets')">True</SSDTExists>
	<VisualStudioVersion Condition="'$(SSDTExists)' == ''">11.0</VisualStudioVersion>
  </PropertyGroup>
  <Import Condition="'$(SQLDBExtensionsRefPath)' != ''" Project="$(SQLDBExtensionsRefPath)\Microsoft.Data.Tools.Schema.SqlTasks.targets" />
  <Import Condition="'$(SQLDBExtensionsRefPath)' == ''" Project="$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v$(VisualStudioVersion)\SSDT\Microsoft.Data.Tools.Schema.SqlTasks.targets" />
  <ItemGroup>
	<Folder Include="Properties" />
	<Folder Include="dbo\" />
	<Folder Include="dbo\Tables\" />
	<Folder Include="Security\" />
	<Folder Include="Security\Master Keys\" />
	<Folder Include="Security\Certificates\" />
	<Folder Include="Security\Symmetric Keys\" />
	<Folder Include="Scripts\" />
	<Folder Include="Scripts\PostDeployment\" />
	<Folder Include="Scripts\PostDeployment\Data\" />
  </ItemGroup>
  <ItemGroup>
	<Build Include="dbo\Tables\Categories.sql" />
	<Build Include="dbo\Tables\Tasks.sql" />
	<Build Include="dbo\Tables\USERS.sql" />
	<Build Include="dbo\Tables\ROLES.sql" />
	<Build Include="dbo\Tables\USER_ROLES.sql" />
	<Build Include="dbo\Tables\SalesData.sql" />
	<Build Include="dbo\Tables\ROLE_PERMISSIONS.sql" />
	<Build Include="dbo\Tables\ROLE_PERMISSION_MAPPING.sql" />
  </ItemGroup>
  <ItemGroup>
	<None Include="Security\Master Keys\CreateMasterKey.sql" />
	<None Include="Security\Certificates\DataEncryptionCert.sql" />
	<None Include="Security\Symmetric Keys\DataEncryptionKey.sql" />
	<None Include="Scripts\PostDeployment\Data\CreateDefaultUser.sql" />
	<None Include="Scripts\PostDeployment\Data\CreateDefaultRoles.sql" />
  </ItemGroup>
  <ItemGroup>
	<PreDeploy Include="Scripts\Script.PreDeployment.sql" />
  </ItemGroup>
  <ItemGroup>
	<PostDeploy Include="Scripts\Script.PostDeployment.sql" />
  </ItemGroup>
</Project>
'@

Set-Content -Path $projectFile -Value $newContent -Encoding UTF8
Write-Host "Project file updated successfully!" -ForegroundColor Green
Write-Host "Please reopen the solution in Visual Studio." -ForegroundColor Yellow
