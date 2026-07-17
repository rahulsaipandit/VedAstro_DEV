using System;

namespace VedAstro.Library;

/// <summary>
/// Holds all URLs used by the Website/ViewComponents layer: fixed public/social links (static),
/// and API endpoint paths that vary by runtime (beta/stable) & local-dev debug mode (instance).
/// Reconstructed from scratch based on caller usage - see Library/Logic/Calculate/CoreTime.cs
/// header note for the equivalent situation in the Calculate facade.
/// </summary>
public class URL
{
    //STATIC - fixed public links, same regardless of runtime/debug mode
    public const string WebStable = "https://vedastro.org";
    public const string WebStableDirect = "https://vedastrowebsitestorage.z5.web.core.windows.net";
    public const string WebBeta = "https://beta.vedastro.org";
    public const string ApiStable = "https://api.vedastro.org/api";
    public const string ApiBeta = "https://beta.api.vedastro.org/api";
    public const string ApiLocalDebug = "http://localhost:7071/api";

    public const string KoFiPage = "https://ko-fi.com/vedastro";
    public const string PatreonPage = "https://www.patreon.com/vedastro";
    public const string Twitter = "https://twitter.com/vedastro";
    public const string Instagram = "https://www.instagram.com/vedastro.official";
    public const string YoutubeChannel = "https://www.youtube.com/@vedastro";
    public const string FacebookPage = "https://www.facebook.com/vedastro.official";
    public const string GitHubRepo = "https://github.com/VedAstro/VedAstro";
    public const string HuggingFaceRepo = "https://huggingface.co/VedAstro";
    public const string DesktopAppDownload = "https://vedastro.org/Download.html";
    public const string ExcelSampleMLFile = "https://vedastro.org/data/SampleMLFile.xlsx";

    //MISC EXTERNAL REFERENCE LINKS - wiki/youtube/social links used in article/prediction text
    public const string BVRamanWiki = "https://en.wikipedia.org/wiki/Bangalore_Venkata_Raman";
    public const string LaSalleWiki = "https://en.wikipedia.org/wiki/Robert_DeLuce";
    public const string NasaJplSource = "https://ssd.jpl.nasa.gov/";
    public const string YukteswarWiki = "https://en.wikipedia.org/wiki/Sri_Yukteswar";
    public const string WHAudenWiki = "https://en.wikipedia.org/wiki/W._H._Auden";
    public const string StarsThatDontGiveDam = "https://vedastro.org/Blog/StarsThatDontGiveADamn.html";
    public const string SwamiRamaWiki = "https://en.wikipedia.org/wiki/Swami_Rama";
    public const string MahatmaGandhiWiki = "https://en.wikipedia.org/wiki/Mahatma_Gandhi";
    public const string JohnLenonImagine = "https://en.wikipedia.org/wiki/Imagine_(John_Lennon_song)";
    public const string CarlSaganWiki = "https://en.wikipedia.org/wiki/Carl_Sagan";
    public const string MiltonFriedmanWiki = "https://en.wikipedia.org/wiki/Milton_Friedman";
    public const string FreedmanYoutubePencil = "https://www.youtube.com/watch?v=67tHtpac5ws";

    //CONTACT / COMMUNITY LINKS
    public const string WhatsAppContact = "https://wa.me/message/vedastro";
    public const string TelegramContact = "https://t.me/vedastro";
    public const string SlackInviteURL = "https://join.slack.com/t/vedastro/shared_invite";
    public const string GitHubIssues = "https://github.com/VedAstro/VedAstro/issues";
    public const string GitHubCommits = "https://github.com/VedAstro/VedAstro/commits/master";
    public const string GitHub88000Lines = "https://github.com/VedAstro/VedAstro";
    public const string GitDeveloperRoomProject = "https://github.com/orgs/VedAstro/projects";
    public const string GitHubDemoFiles = "https://github.com/VedAstro/VedAstro/tree/master/Demo";
    public const string JHoraEasyImportYoutube = "https://www.youtube.com/@vedastro";
    public const string APIGuideNextStep = "https://vedastro.org/Docs/QuickGuide.html";

    //DONATION / PAYMENT LINKS
    public const string KoFiDonateIframe = "https://ko-fi.com/vedastro/?hidefeed=true&widget=true&embed=true&preview=true";
    public const string TelescopeBuyPage = "https://vedastro.org/Donate.html";
    public const string PaypalMePage = "https://paypal.me/vedastro";
    public const string KoFiSponsorMemberships = "https://ko-fi.com/vedastro/tiers";
    public const string KoFiPrivateServer = "https://ko-fi.com/vedastro/tiers";

    //MISC PAGE LINKS
    public const string VSLifeSharePublicSession = "https://vedastro.org/VSLifeSharePublicSession.html";

    /// <summary>Free IP-based geolocation lookup, used to guess a visitor's default location.</summary>
    public const string GeoJsApiUrl = "https://get.geojs.io/v1/ip/geo.json";


    //INSTANCE - varies by runtime (beta/stable) & local-dev debug mode

    /// <summary>Base API URL used for direct (non-relative) calls from the client.</summary>
    public string ApiUrlDirect { get; }

    /// <summary>Base website URL for the current runtime (beta or stable).</summary>
    public string WebUrl { get; }

    public URL(bool isBetaRuntime, bool debugMode)
    {
        if (debugMode)
        {
            ApiUrlDirect = ApiLocalDebug;
        }
        else
        {
            ApiUrlDirect = isBetaRuntime ? ApiBeta : ApiStable;
        }

        WebUrl = isBetaRuntime ? WebBeta : WebStable;
    }

    //API ENDPOINTS - each is simply "{ApiUrlDirect}/{EndpointName}", callers append their own path segments

    public string GetPersonList => $"{ApiUrlDirect}/GetPersonList";
    public string AddPerson => $"{ApiUrlDirect}/AddPerson";
    public string DeletePerson => $"{ApiUrlDirect}/DeletePerson";
    public string UpdatePerson => $"{ApiUrlDirect}/UpdatePerson";
    public string UpsertLifeEvent => $"{ApiUrlDirect}/UpsertLifeEvent";
    public string GetNewPersonId => $"{ApiUrlDirect}/GetNewPersonId";
    public string GetPerson => $"{ApiUrlDirect}/GetPerson";
    public string GetPersonImage => $"{ApiUrlDirect}/GetPersonImage";
    public string AddTaskApi => $"{ApiUrlDirect}/AddTask";

    public string FindMatch => $"{ApiUrlDirect}/FindMatch";
    public string GetMatchReportList => $"{ApiUrlDirect}/GetMatchReportList";

    public string GetEventsChart => $"{ApiUrlDirect}/GetEventsChart";
    public string GetEventsApi => $"{ApiUrlDirect}/GetEvents";
    public string GetSavedEventsChartIdList => $"{ApiUrlDirect}/GetSavedEventsChartIdList";
    public string GetPersonIdFromSavedChartId => $"{ApiUrlDirect}/GetPersonIdFromSavedChartId";
    public string DeleteChartApi => $"{ApiUrlDirect}/DeleteChart";

    public string GetTimezoneOffsetApi => $"{ApiUrlDirect}/Calculate/GeoLocationToTimezone";
    public string HoroscopePredictions => $"{ApiUrlDirect}/HoroscopePredictions";
    public string HoroscopeLLMSearch => $"{ApiUrlDirect}/Calculate/HoroscopeLLMSearch";
    public string GetVisitorList => $"{ApiUrlDirect}/GetVisitorList";
    public string AddMessageApi => $"{ApiUrlDirect}/AddMessage";

    public string SignInGoogle => $"{ApiUrlDirect}/SignInGoogle";
    public string SignInFacebook => $"{ApiUrlDirect}/SignInFacebook";
    public string Login => $"{ApiUrlDirect}/Login";

    public string GetMatchReportApi => $"{ApiUrlDirect}/GetMatchReport";
    public string SaveMatchReportApi => $"{ApiUrlDirect}/SaveMatchReport";

    public string GetMessageList => $"{ApiUrlDirect}/GetMessageList";
    public string GetTaskListApi => $"{ApiUrlDirect}/GetTaskList";
    public string DeleteVisitorByUserId => $"{ApiUrlDirect}/DeleteVisitorByUserId";
    public string DeleteVisitorByVisitorId => $"{ApiUrlDirect}/DeleteVisitorByVisitorId";

    /// <summary>Google Identity Services JS SDK, used to render the "Sign in with Google" button.</summary>
    public const string GoogleSignInJs = "https://accounts.google.com/gsi/client";
}
