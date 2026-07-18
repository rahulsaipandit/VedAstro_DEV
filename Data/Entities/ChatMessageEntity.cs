using System;

namespace VedAstro.Library
{
    /// <summary>
    /// Plain POCO - physically lives in VedAstro.Data (not Library) so the Data project doesn't
    /// need a reference to Library (Library references Data, not the reverse - see
    /// PersonListEntity.cs's header comment for the same reasoning). The row-key hashing
    /// (Tools.GetStringHashCodeMD5/GenerateId, Time.ToUrl) and message-numbering
    /// (ChatAPI.GetLastMessageNumberNumberFromSessionId) that used to live in a constructor here
    /// are Library-only concerns, so they now live in ChatAPI.CreateChatMessage(...)
    /// (Library/Logic/Calculate/ChatAPI.cs) instead.
    /// </summary>
    public class ChatMessageEntity : IPartitionRowKeyEntity
    {
        /// <summary>
        /// session id
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// text hash + user birth
        /// </summary>
        public string RowKey { get; set; }

        public string Sender { get; set; }

        public string Text { get; set; }
        public int Rating { get; set; }
        public int MessageNumber { get; set; }
        public string UserId { get; set; }

        /// <summary>
        /// mandatory
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }
    }
}
