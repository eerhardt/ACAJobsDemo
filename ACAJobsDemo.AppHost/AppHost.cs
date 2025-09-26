#pragma warning disable ASPIREAZURE002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var storageQueue = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddQueue("myqueue");

var processor = builder.AddProject<Projects.ACAJobsDemo_ProcessorJob>("processor")
    .WithReference(storageQueue).WaitFor(storageQueue);
processor
    .PublishAsAzureContainerAppJob((infra, job) =>
    {
        var accountNameParameter = storageQueue.Resource.Parent.Parent.NameOutputReference.AsProvisioningParameter(infra);

        if (!processor.Resource.TryGetLastAnnotation<AppIdentityAnnotation>(out var identityAnnotation))
        {
            throw new InvalidOperationException("Identity annotation not found.");
        }

        job.Configuration.TriggerType = ContainerAppJobTriggerType.Event;
        job.Configuration.EventTriggerConfig.Scale.Rules.Add(new ContainerAppJobScaleRule
        {
            Name = "queue-rule",
            JobScaleRuleType = "azure-queue",
            Metadata = new ObjectExpression(
                new PropertyExpression("accountName", new IdentifierExpression(accountNameParameter.BicepIdentifier)),
                new PropertyExpression("queueName", new StringLiteralExpression(storageQueue.Resource.QueueName)),
                new PropertyExpression("queueLength", new IntLiteralExpression(1))
            ),
            Identity = identityAnnotation.IdentityResource.Id.AsProvisioningParameter(infra)
        });
    });

builder.AddProject<Projects.Frontend>("frontend")
    .WithExternalHttpEndpoints()
    .WithReference(storageQueue);

builder.Build().Run();
