# Keep one backend deployable with enforced source modules

Amaara's checkout, inventory and payment history rely on local SQL transactions and one deployable. Keep `be/` and `client/` and establish API, application, domain and infrastructure ownership inside the existing backend project, enforced by architecture tests. Splitting assemblies or moving deployment roots would add migration, hosting and test churn now; a future assembly split can follow these established interfaces if independent compilation becomes valuable.
