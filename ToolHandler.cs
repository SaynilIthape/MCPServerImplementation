namespace MCPImplementation
{
    public static class ToolHandler
    {
        public static string Execute(string toolName, string argument)
        {
            if (toolName == "get_discount")
            {
                return GetDiscount(argument);
            }
            else if (toolName == "get_price")
            {
                return GetPrice(argument);
            }

                return "Unknown tool";
        }

        private static string GetDiscount(string product)
        {
            return product.ToLower() switch
            {
                "laptop" => "10% discount",
                "mobile" => "5% discount",
                _ => "No discount"
            };
        }

        private static string GetPrice(string product)
        {
            return product.ToLower() switch
            {
                "laptop" => "100",
                "mobile" => "50",
                _ => "No price"
            };
        }
    }
}
