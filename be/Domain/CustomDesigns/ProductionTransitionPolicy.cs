namespace be.Domain.CustomDesigns;
public static class ProductionTransitionPolicy
{
    public static bool CanTransition(ProductionStatus current, ProductionStatus next) => (current, next) switch
    {
        (ProductionStatus.NotStarted, ProductionStatus.Queued) => true,
        (ProductionStatus.Queued, ProductionStatus.Printing) => true,
        (ProductionStatus.Printing, ProductionStatus.Finishing) => true,
        (ProductionStatus.Finishing, ProductionStatus.QualityCheck) => true,
        (ProductionStatus.QualityCheck, ProductionStatus.Ready) => true,
        _ => false
    };
}
