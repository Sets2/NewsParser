namespace NewsParser.Models;

/// <example>
///{
///    "IsReaded": true
///}
/// </example>

public class CreateEditItemRequest
{
    public bool IsReaded { get; set; } = false;
}