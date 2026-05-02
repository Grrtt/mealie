using Mealie.Application.Contracts.Search;
using Mealie.Application.Services.ImageScrape;
using Mealie.Application.Services.Parser;
using Mealie.Infrastructure.Admin;
using NlpParserService = Mealie.Application.Services.IngredientParser.IngredientParserService;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Webhooks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Queries;

/// <summary>
///     Common services made available to every query via <see cref="QueryExecutor" />.
///     Add shared cross-cutting services here (logging, caching, etc.) as the need
///     arises rather than threading them through every query constructor.
/// </summary>
public interface IQueryServices
{
    ApplicationDbContext Db { get; }
    ITenantContext Tenant { get; }
    IMediator Mediator { get; }
    ILoggerFactory LoggerFactory { get; }
    IOptions<AppSettings> Settings { get; }
    NlpParserService IngredientParser { get; }
    IIngredientParserService FullParser { get; }
    ImageScrapeQueue ImageScrapeQueue { get; }
    IWebhookDeliveryService WebhookDeliveryService { get; }
    IRecipeSearchIndex SearchIndex { get; }
    IApiKeyEncryptionService EncryptionService { get; }
}
