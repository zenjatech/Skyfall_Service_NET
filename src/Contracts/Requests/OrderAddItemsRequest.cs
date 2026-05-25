namespace Skyfall.Contracts.Requests;

public sealed class OrderAddItemsRequest
{
    public List<OrderItemRequest> Items { get; set; } = [];
}
