# AWSLauncher

Deployment System Based on AWS Lambda, latest release can be found [here](https://github.com/asmodat/AWSLauncher/releases).

## Installing Debug Tools

> https://github.com/aws/aws-lambda-dotnet/tree/master/Tools/LambdaTestTool

```
dotnet tool install -g Amazon.Lambda.TestTool-2.1
dotnet tool update -g Amazon.Lambda.TestTool-2.1
```

Set env variable `AWS_PROFILE` to your credential profile on local. 


## Setup Lambda Function

> Wizard

```
Function name: AWSLauncher
Runtime: .NET Core 2.1 (C#/PowerShell)
Choose or create an existing role -> existing role -> (create role with permissions to secrets and ec2)
```

> Add Trigger

```
CloudWatch Event
Rule -> Create new rule
	Rule name -> AWSLauncher-Trigger
	Schedule expression -> rate(1 minute)
	Enable trigger -> yes
```

> AWS Launcher

```
Memory: 256 MB
Timeout: 15 min
Network -> depending on your security requirements
```

> Environment variables

```
parallelism: 1
github_branch: master
github_user: <username>
github_token: <name_of_secret_token> 
github_repository: <repo_name>
#folder within github repository, e.g.
instances_path: AWS/<region>/EC2/Instances
```

> Function code

```
Code entry type -> .zip -> (execute ./publish.sh script to generate)
Runtime: .NET Core 2.1 (C#/PowerShell)
Handler: AWSLauncher::AWSLauncher.Function::FunctionHandler
```

## Example Deployment File

> placed in: `github.com/<username>/<repo_name>/AWS/<region>/EC2/Instances/config.json`

```
{
    "enabled": true,
    "name":"Example_instance",
    "instanceType": "t3a.micro",
    "imageId": "ami-xxxxxxxxxxxxxx",
    "securityGroups": [ "Security_Group_Name" ],
    "subnetId":"subnet-yyyyyyyyy",
    "roleName": "Role_Name_To_Be_Assumed_By_Instance",
    "key": "SSH_Key_Name",
    "rootVolumeSize":16,
    "restart": 0,
    "redeploy": 0,
    "terminate": false,
    "shutdown": false,
    "tags": [
        { "key":"test", "value":"value" },
        { "key":"test2", "value":"value2" }
    ]
}
```

## Netowrking

> Lambdas suffer from some networking issues especially when working with private subnets
> For testing it is recommended to use NoVPC, while on mainnet propper configuration of VPC, subnets, routes, gateway & nat along endpoints is required
> Following resources might be helpfull when dealing with the issues.

```
https://forums.aws.amazon.com/thread.jspa?threadID=279633
https://gist.github.com/reggi/dc5f2620b7b4f515e68e46255ac042a7
https://docs.aws.amazon.com/vpc/latest/userguide/vpc-dns.html
https://www.oodlestechnologies.com/blogs/How-to-grant-internet-access-to-AWS-Lambda-under-VPC/
```




