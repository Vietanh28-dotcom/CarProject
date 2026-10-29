namespace CarProject.Services;

public static class DepositCalculator
{
    public const decimal InStockRate = 0.20m;
    public const decimal PreOrderRate = 0.15m;

    public static decimal Compute(long giaNiemYet, bool isPreOrder)
        => Math.Round(giaNiemYet * (isPreOrder ? PreOrderRate : InStockRate) / 1_000_000) * 1_000_000;

    public static decimal ComputeByQuantity(long giaNiemYet, int totalQuantity)
        => Math.Round(giaNiemYet * (totalQuantity <= 2 ? PreOrderRate : InStockRate) / 1_000_000) * 1_000_000;
}
