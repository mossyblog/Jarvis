# [J]ust [A] [R]ather [V]ery [I]ntelligent [S]ystem

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

JARVIS is an open source Entity-Component-System (ECS) framework designed to help developers create and manage data in their applications. It is available as a NuGet package and can be used in any .NET application.  JARVIS is based on the ECS pattern, but it has been adapted to suit the specific needs of its application. Instead of using the standard decorator pattern to add components to entities at runtime, JARVIS uses a factory pattern to ensure that entities are created with the correct properties and methods. Additionally, the ECS pattern is used to create a normalized model of the data instead of a hierarchical structure.  JARVIS provides developers with a powerful and flexible way to manage their data. It is designed to be easy to use and understand, and it is highly extensible. It also provides a number of features to help developers create and maintain their applications, such as a data modeler, a query builder, and a data validation system.  JARVIS is an ideal choice for developers who need to create and manage data in their applications. It is open source, so developers can customize it to suit their needs, and it is available as a NuGet package, so it can be easily integrated into any .NET application. With its powerful features and flexible design, JARVIS is the perfect choice for developers who need to create and manage data in their applications.

## Getting Started

Our Getting Started tutorial walks you through integrating Jarvis with a simple application and gives you some starting points for learning more.

Super-duper quick start:

```
Jarvis.Initialize();
```

If you wish to integrate with your own AutoFac Builder, use the following:

```
var builder = new ContainerBuilder();

builder.Register(c => new TaskController(c.Resolve<ITaskRepository>()));
builder.RegisterType<TaskController>();
builder.RegisterInstance(new TaskController());
builder.RegisterAssemblyTypes(controllerAssembly);

// Integrate with an existing Autofac Builder thats external.
Jarvis.Register(builder);

builder.Build();

```

Jarvis will attach its necessary registeries to your builder so you are able to hook it into place.

### Prerequisites

JARVIS requires .NET 6.0 or above, in addition to the Autofac and Serilog libraries. Autofac is an open source dependency injection library which allows developers to register components and services in a modular fashion, and resolve them when needed. Serilog is a logging library used to log events from applications.

In order to use JARVIS, the FJarvis class must be used as a singleton.

```
Jarvis.EntitiesGive examples
```

### Need Help?

**Need help with Jarvis?** We will have a [a documentation site](https://fjarvis.readthedocs.io/) as well as API Documentation [Comming Soon]!

## Get Packages

You can get Autofac by [grabbing the latest NuGet package](https://www.nuget.org/packages/FJarvis/). If you're feeling adventurous, [continuous integration builds are on MyGet](https://www.myget.org/gallery/FJarvis).

[Release notes are available on GitHub](https://github.com/mossyblog/FJarvis/releases).

## Running the tests

Using either Jetbrains Rider or Visual Studio, each assembly wihtin Jarvis has a standard NUnit Test shadow project. Simply execute Run All Unit Tests in the solution to validate the framework.

## Contributing / Pull Requests

If you are interested in contributing to the Jarvis project, we welcome and deeply appreciate your help. Your contributions can benefit the whole user community and help ensure the smooth running of the project.  We have implemented the following guidelines to maintain a consistent standard across the codebase. While you are free to deviate from these guidelines should you feel it is necessary, we ask that you stick to them as closely as possible.  If you are interested in contributing code or documentation, please submit a pull request. We also welcome questions and suggestions.

When making contributions to the project, please ensure you are filing issues and submitting pull requests in the appropriate repository. For example, if the issue is with the Jarvis integration, it should be filed in the corresponding repository, and not the core Jarvis repo.  To ensure successful contributions, please adhere to the following steps:

* **File an issue**: Whether you are suggesting a feature or noting a defect, please provide a detailed description of the challenge you are facing and how you believe the feature should work. If you are reporting a defect, please include a description as well as steps to reproducing the issue (ideally including one or more failing unit tests).
* **Design discussion**: For new features, discussion may take place in the issue to determine whether it is something that should be included with Jarvis or be a user-supplied extension. For defects, discussion may happen around whether the issue is truly a defect or if the behavior is correct.
* **Pull request**: Create a pull request on the develop branch of the repository to submit changes to the code based on the information in the issue. All pull requests must pass the CI build and follow coding standards. Please note that all pull requests should also include accompanying unit tests to verify the work.
* **Code review**: After submitting a pull request, it may be necessary to make some updates (e.g. to fix a typo or add error handling).
* **Pull request acceptance**: Once the pull request has been accepted into the develop branch and pushed to the master branch, it will be included in the next release.

## License

Jarvis is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

By contributing to Jarvis, you assert that:

1. The contribution is your own original work.
2. You have the right to assign the *copyright* for the work (it is not owned by your employer, or you have been given copyright assignment in writing).
3. You license it under the terms applied to the rest of the Jarvis project.

## Built With

Normal .NET coding guidelines apply. See the [Framework Design Guidelines](https://msdn.microsoft.com/en-us/library/ms229042.aspx) for suggestions. We have Roslyn analyzers running on most of the code. These analyzers are actually correct a majority of the time. Please try to fix warnings rather than suppressing the message. If you do need to suppress a false positive, use the `[SuppressMessage]` attribute.

Jarvis source code uses four spaces for indents. We use [EditorConfig](https://editorconfig.org/) to ensure consistent formatting in code docs. Visual Studio has this built in since VS 2017. VS Code requires the EditorConfig extension. Many other editors also support EditorConfig.

## Contributing

Please read [CONTRIBUTING.md](https://github.com/your/project/CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Code Documentation and Examples

It is *strongly* encouraged that you update the Jarvis documentation when making changes. If your changes impact existing features, the documentation may be updated regardless of whether a binary distribution has been made that includes the changes.

You should also include XML API comments in the code. These are used to generate API documentation as well as for IntelliSense.

**The Golden Rule of Documentation: Write the documentation you'd want to read.** Every developer has seen self explanatory docs and wondered why there wasn't more information. (Parameter: "index." Documentation: "The index.") Please write the documentation you'd want to read if you were a developer first trying to understand how to make use of a feature.

For new integrations or changes to existing integrations, you may need to add or update [the examples repo](https://github.com/FJarvis/Examples) to show how the integration works.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/mossyblog/FJarvis/master).

## Authors

* **Scott Barnes** - *Initial work* - [Mossyblog Github](https://github.com/mossyblog)
