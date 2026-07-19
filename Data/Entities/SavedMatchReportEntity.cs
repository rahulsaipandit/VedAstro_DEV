using System;

namespace VedAstro.Library
{
    /// <summary>
    /// Backs the old Blazor site's GetMatchReportList/SaveMatchReport pair (referenced by
    /// ViewComponents/Code/Managers/WebsiteTools.cs and Website/Pages/Calculator/Match/SavedReports.razor)
    /// which never had a real server-side implementation - the Azure Function endpoint the XML client
    /// called against never existed, and Phase 1+2 of the migration only carried over the read-only
    /// FindMatch/GetMatchReport endpoints. This entity is genuinely new persistence, not a straight
    /// Azure-Table-to-Postgres port.
    /// </summary>
    public class SavedMatchReportEntity : IPartitionRowKeyEntity
    {
        /// <summary>Owner of the saved report - real UserId when signed in, VisitorId ("101" guest sentinel scheme) otherwise.</summary>
        public string PartitionKey { get; set; }

        /// <summary>"{MaleId}_{FemaleId}" - one saved report per couple per owner (re-saving just updates Notes/SavedAt).</summary>
        public string RowKey { get; set; }

        public string MaleId { get; set; }
        public string FemaleId { get; set; }
        public string Notes { get; set; }
        public DateTimeOffset? SavedAt { get; set; }
    }
}
