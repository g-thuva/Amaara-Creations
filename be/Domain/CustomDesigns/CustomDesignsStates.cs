namespace be.Domain.CustomDesigns;
public enum CustomDesignStatus
{
    Draft = 0,
    Ready = 1,
    InCart = 2,
    Ordered = 3,
    Archived = 4
}

public enum BuilderVersionStatus
{
    Draft = 0,
    Published = 1,
    Superseded = 2
}

public enum BuilderOptionGroup
{
    Shape = 0,
    Material = 1,
    Finish = 2,
    Font = 3,
    Colour = 4
}

public enum ProofStatus
{
    NotRequired = 0,
    Preparing = 1,
    AwaitingApproval = 2,
    ChangesRequested = 3,
    Approved = 4
}

public enum ProofRevisionStatus
{
    AwaitingApproval = 0,
    ChangesRequested = 1,
    Approved = 2,
    Superseded = 3
}

public enum ProductionStatus
{
    NotStarted = 0,
    Queued = 1,
    Printing = 2,
    Finishing = 3,
    QualityCheck = 4,
    Ready = 5
}
