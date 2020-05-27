# Reference .NET Core API Project

Contains a reference project for a .NET Core API project, intended for use as a quick start guide and example of best practices as defined by the Platform Engineering team.

## The Project

This is a bare-bones example of a .NET 3.1 API distributed in a Docker image. It has all the logic contained in the `DemoApi.Services` namespace, and has an associated test project using the XUnit testing framework. The application artifacts are packaged into a Docker image based on the official .NET runtime image, which is versioned using GitVersion in mainline mode. The Docker image is then pushed to an Amazon ECR repository. A basic Helm 2 chart is packaged with the new version of the image, and sent to an S3 bucket in AWS. 

There is also Terraform configuration, which sets up a DNS endpoint for the application, as well as setting default retention policies for the ECR repository that contains the Docker images.

The deployment pipeline is defined by Spinnaker's pipeline templating specification. The `spinnaker-template.json` file defines a pipeline template - that is, a parameterized set of steps. The `spinnaker-pipeline.json` file implements the pipeline template - it provides the actual values for the pipeline.

The Spinnaker pipeline plans and applies the Terraform configuration, then uses the Terraform outputs and other provided overrides to template and deploy the Helm chart in two environments: development and production. These environments are two different namespaces in the same EKS cluster for demo purposes, but in a real implementation they will likely be in different clusters in different AWS accounts.

## Versioning

This repository uses [mainline](https://gitversion.net/docs/reference/versioning-modes/mainline-development) versioning, calculated using [GitVersion](https://github.com/GitTools/GitVersion). The version is calculated off of an initial tag. Major & minor versions may be bumped either by adding additional tags, or by using conventional commit messages. Mainline, also called [trunk based development](https://trunkbaseddevelopment.com/), is the Platform Engineering team's preferred branching/versioning system.

## Code Coverage

This project uses [Coverlet](https://github.com/tonerdo/coverlet), specifically the [MSBuild integration](https://github.com/tonerdo/coverlet/blob/master/Documentation/MSBuildIntegration.md) using the [Cobertura](https://github.com/cobertura/cobertura) report format, as its code coverage tool. It then uses the [Code Coverage API Jenkins Plugin](https://jenkins.io/blog/2018/08/17/code-coverage-api-plugin-1/) to display and track the coverage results. By default, `failBuildIfCoverageDecreasedInChangeRequest` is set to true, meaning that Pull Requests must meet or exceed the current coverage levels in the `master` branch.

## Setting Up Jenkins

Prerequisites for Jenkins:

* Jenkins must be [configured to work with GitVersion](https://confluence.hyland.com/x/VQeX).  
  * GitVersion must be version 4.x - there are breaking changes in version 5.x. 
* The Jenkins secret used for pushing the image to an AWS repository must be an `AWS Credentials` secret. Alternate ways of providing this credential are discussed in the [trebuchet documentation](https://github.com/HylandSoftware/trebuchet).
* There must be a Jenkins secret file that contains the `config` file for using the [Spin CLI](https://www.spinnaker.io/setup/spin/) to update the Spinnaker pipeline.

## Setting up the Deployment

Prerequisites for deployment:

* An AWS account, configured in Spinnaker
* An EKS Kubernetes cluster, configured in Spinnaker, created using standard Platform Engineering Terraform modules, which should include configuration for:
  * A public facing Network Load Balancer (NLB)
  * A Route53 DNS alias for the NLB
  * An nginx ingress controller
* An ECR Docker registry, configured in Spinnaker
* An S3 bucket for storing Helm charts, configured as an artifact source in Spinnaker
* Namespaces for development and production environments

Ideally, all of these prerequisites except for the namespaces will be automatically configured either as a part of AWS account creation, or with a request to the Platform Engineering team. However, for now, work with the PE team to make sure that all the pieces are configured. 

### Spinnaker pipelines

**NOTE:** Spinnaker is a new tool to us. As such, the recommendations and processes around it are still a work in progress. Keep an eye out for changing documentation and best practice recommendations as we solidify our Spinnaker strategy.

There are two files for the Spinnaker pipeline. The first, `spinnaker-template.json`, provides a full description of the pipeline stages, as well as defining variables that are used throughout the pipeline. It is unlikely that you will edit this file at all. **IMPORTANT**: If you do update the template file, make sure to change the `id` field on line 3, or else you will be writing over the original template when it is saved to Spinnaker, causing unintended changes in other people's pipelines. 

The second file, `spinnaker-pipeline.json`, provides the specific values for your particular application. This file should be updated to reflect your application, especially the `application` and `name` fields. The `application` field refers to the Spinnaker application construct. This will almost always be the same as your Helm deployment name. The `name` field refers to the pipeline name in Spinnaker. One application may have several pipelines in Spinnaker. 

Currently, the workflow is to save the `spinnaker-template.json` file to Spinnaker before the `spinnaker-pipeline.json` file is saved, so that the pipeline file can reference the template correctly. However, this workflow is likely to change, as template files are really more intended to be maintained in a central repository, and used by many applications, rather than maintained individually in specific application repositories. 

## Using this repository as a template

This repository will work as a basic template for .NET Core projects. To simply recreate this repository in your team's Bitbucket project, follow these directions:

1. Clone this repository to your local workspace
2. Delete the `.git` directory to remove the history and decouple it from the reference repository
3. Create a blank repository in the appropriate Bitbucket project 
4. Follow the instructions in the `My code is ready to be pushed` section in the empty repository configuration
5. Tag the initial commit with a version and push it - `git push origin --tags`

At minimum, the `AWS_CREDENTIALS` environment variable and `spin-config` credentialsId in the Jenkinsfile will need updated to use the correct Jenkins secrets for AWS credentials and Spin CLI credentials, respectively. Alternately, the master-branch-only stages will need disabled.

The following files & directories will probably need renamed:

* DemoSolution.sln
* src/DemoApi (dir)
* DemoApi.csproj
* DemoApi.Tests (dir)
* DemoApi.Tests.csproj
* deploy/demo-api

Renaming any one of these will require several changes to ensure that the changes propagate correctly to all their references in the solution file, csproj file(s), and to the namespaces. To update the project files:

1. Open the solution in Visual Studio
2. Right-click on each of the projects, select `Rename` and update the names. Be sure to save in Visual Studio afterwards.
3. In the `Program.cs` file, right-click on the namespace `DemoApi`, and select `Rename...` to update the namespace
4. Make sure everything is saved and exit Visual Studio

At this point, everything should still build locally. However, the project directories `DemoApi` and `DemoApi.Tests` have not been renamed. There is no easy way to update these in Visual Studio that catches all of the references. 

5. Open a PowerShell command prompt, and make sure your working directory is the `src` directory
6. Delete the `DemoSolution.sln` file - `rm .\DemoSolution.sln`
7. Remove the project reference from the test project - `dotnet remove <path\to\test\project> reference <path\to\api\project>`
8. Run the `Rename-Item` cmdlet on each of the project directories
9. Re-add the project reference to the test project - `dotnet add <path\to\test\project> reference <path\to\api\project>`
10. Recreate the solution - `dotnet new sln -n <NewSolutionName>`
11. Re-add the projects to the solution - `dotnet sln add <path\to\project>`

Update the Helm charts as follows:

12. Rename the `deploy/demo-api` directory
13. Update the chart name in the `Chart.yaml` file
14. Optionally, update all references to `demo-api` throughout the Helm template files. The easiest way to do this is with a Goland IDE, and refactor the definitions in the `_helpers.tpl` file. Otherwise, use your editor of choice to find and replace all references.

Update the Terraform:

15. Update the values in the following files:

    * `dev.tfvars`
    * `devBackend.tfvars`
    * `prod.tfvars`
    * `prodBackend.tfvars`

Update the Spinnaker Pipeline:

16. Update the `application`, `name`, and `variables` fields in the `spinnaker-pipeline.json` file

The Dockerfile and Jenkinsfile will still need updated. Follow the `TODO` comments in these files to update the appropriate values in each. 

It may be simpler to recreate the whole application project and Helm chart from scratch, using the `dotnet` and `helm` command lines. However, if you go that route, make sure you understand the interaction between the application, the Helm chart, the Jenkinsfile, and the Dockerfile before you start, and are able to recreate the pipeline logic. 