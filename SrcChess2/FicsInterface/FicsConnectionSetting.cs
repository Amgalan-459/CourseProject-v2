namespace SrcChess2.FicsInterface {

    /// <summary>
    /// FICS Connection setting
    /// </summary>
    /// <remarks>
    /// Ctor
    /// </remarks>
    /// <param name="hostName"> Host name</param>
    /// <param name="hostPort"> Host port</param>
    /// <param name="userName"> User name</param>
    public class FicsConnectionSetting(string hostName, int hostPort, string userName) {

        /// <summary>
        /// FICS Server Host Name
        /// </summary>
        public string HostName { get; set; } = hostName;

        /// <summary>
        /// FICS Server Host port
        /// </summary>
        public int HostPort { get; set; } = hostPort;

        /// <summary>
        /// true for anonymous, false for rated
        /// </summary>
        public bool Anonymous { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; } = userName;
    }
}
