namespace VedAstro.Library
{
    /// <summary>
    /// Hindu festivals whose date is computed fresh each year from tithi/lunar-month rules (see
    /// Calculate.FestivalDate), not read from a static per-year lookup table.
    /// </summary>
    public enum FestivalName
    {
        /// <summary>Chaitra Shukla Navami</summary>
        Ramnavami,

        /// <summary>Phaalguna Krishna Pratipada (the day after Phaalguna Purnima)</summary>
        Holi,

        /// <summary>Sraavana Purnima</summary>
        Rakshabandhan,

        /// <summary>Kaarteeka Krishna Chaturthi</summary>
        KarwaChauth,

        /// <summary>Kaarteeka Shukla Sashti</summary>
        Chhath,

        /// <summary>Kaarteeka Amavasya</summary>
        Diwali,

        /// <summary>Phaalguna Krishna Chaturdashi</summary>
        MahaShivaratri,

        /// <summary>
        /// Sun's sidereal ingress into Makara (Capricorn) - unlike every other festival here, this
        /// is a purely solar event (not tithi/lunar-month based), so it falls on essentially the
        /// same Gregorian date every year.
        /// </summary>
        MakarSankranti,
    }
}
