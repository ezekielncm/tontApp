namespace Application.PaymentManagement.Queries.VerifierAudit;

using Application.Common;

public sealed record VerifierAuditQuery(Guid TontineId) : IQuery<AuditVerificationResult>;
