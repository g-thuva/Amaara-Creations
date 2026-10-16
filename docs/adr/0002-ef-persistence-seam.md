# Keep explicit EF queries behind the persistence seam

EF Core already provides typed querying, tracking and a unit of work. Application workflows use `IApplicationDbContext` for those capabilities while Infrastructure owns concrete mappings, persistence and the SQL commerce lock. This preserves query/transaction visibility and existing SQL concurrency tests without adding repositories or a second change tracker; application code deliberately references EF query primitives while domain rules remain independent of EF.
