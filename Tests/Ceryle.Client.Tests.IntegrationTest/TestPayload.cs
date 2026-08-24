namespace Ceryle.Client.Tests.IntegrationTest;

public record TestPayload(
    string Value1, 
    DateTime Value2, 
    int Value3, 
    decimal Value4, 
    DateTimeOffset Value5, TestPayload Payload2);