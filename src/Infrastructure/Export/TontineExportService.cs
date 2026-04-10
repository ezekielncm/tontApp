namespace Infrastructure.Export;

using Application.TontineManagement.Services;
using Domain.PaymentManagement.Repositories;
using Domain.TontineManagement.Repositories;
using Domain.TontineManagement.ValueObjects;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

internal sealed class TontineExportService(
    ITontineRepository tontineRepository,
    IVersementRepository versementRepository) : ITontineExportService
{
    public async Task<byte[]> GeneratePdfAsync(Guid tontineId, CancellationToken cancellationToken = default)
    {
        var tontine = await tontineRepository.GetByIdReadOnlyAsync(
            TontineId.From(tontineId), cancellationToken)
            ?? throw new InvalidOperationException("Tontine introuvable.");

        var versements = await versementRepository.GetByTontineAsync(
            TontineId.From(tontineId), cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("TontinesApp").FontSize(18).Bold().FontColor("#1B5E20");
                    col.Item().Text($"Rapport – {tontine.Name}").FontSize(14).SemiBold();
                    col.Item().Text($"Généré le {DateTime.UtcNow:dd/MM/yyyy à HH:mm} UTC")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    // Tontine info
                    col.Item().PaddingBottom(12).Column(info =>
                    {
                        info.Item().Text("Informations générales").FontSize(12).SemiBold();
                        info.Item().Text($"Description : {tontine.Description ?? "—"}");
                        info.Item().Text($"Cotisation : {tontine.ContributionAmount.Amount} {tontine.ContributionAmount.Currency}");
                        info.Item().Text($"Périodicité : {tontine.Periodicity}");
                        info.Item().Text($"Statut : {tontine.Status}");
                        info.Item().Text($"Membres : {tontine.Members.Count} / {tontine.MaxMembers}");
                        info.Item().Text($"Créée le : {tontine.CreatedAt:dd/MM/yyyy}");
                    });

                    // Members table
                    col.Item().PaddingBottom(12).Column(memSec =>
                    {
                        memSec.Item().Text("Membres").FontSize(12).SemiBold();
                        memSec.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("#").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Nom").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Inscrit le").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Rang").Bold();
                            });

                            var i = 1;
                            foreach (var member in tontine.Members)
                            {
                                table.Cell().Padding(4).Text(i.ToString());
                                table.Cell().Padding(4).Text(member.Name);
                                table.Cell().Padding(4).Text(member.JoinedAt.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(4).Text(member.Rang.ToString());
                                i++;
                            }
                        });
                    });

                    // Rounds table
                    col.Item().PaddingBottom(12).Column(roundSec =>
                    {
                        roundSec.Item().Text("Tours").FontSize(12).SemiBold();
                        roundSec.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Tour").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date prévue").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date limite").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Clôturé").Bold();
                            });

                            foreach (var round in tontine.Rounds.OrderBy(r => r.RoundNumber))
                            {
                                table.Cell().Padding(4).Text(round.RoundNumber.ToString());
                                table.Cell().Padding(4).Text(round.ScheduledDate.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(4).Text(round.DateLimite.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(4).Text(round.IsCompleted ? "Oui" : "Non");
                            }
                        });
                    });

                    // Versements table
                    col.Item().Column(versSec =>
                    {
                        versSec.Item().Text("Versements").FontSize(12).SemiBold();
                        versSec.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Référence").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Montant").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Statut").Bold();
                            });

                            foreach (var v in versements.OrderByDescending(x => x.CreatedAt))
                            {
                                table.Cell().Padding(4).Text(v.ReferenceExterne ?? "—");
                                table.Cell().Padding(4).Text($"{v.Montant.Valeur} {v.Montant.Devise}");
                                table.Cell().Padding(4).Text(v.CreatedAt.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(4).Text(v.Statut.ToString());
                            }
                        });
                    });
                });

                page.Footer().AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}
