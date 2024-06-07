using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Linq;
using System.Globalization;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    public static class PgnUtil {

        [Flags]
        private enum PgnAmbiguity {
            NotFound         = 0,
            Found            = 1,
            ColMustBeSpecify = 2,
            RowMustBeSpecify = 4
        }

        public class FilterClause {
            public  bool                        IsAllRanges { get; set; }
            public  bool                        IncludesUnrated { get; set; }
            public  Dictionary<int, int>?       HashRanges { get; set; }
            public  bool                        IncludeAllPlayers { get; set; }
            public  Dictionary<string,string?>? HashPlayerList { get; set; }
            public  bool                        IncludeAllEnding { get; set; }
            public  bool                        IncludeWhiteWinningEnding { get; set; }
            public  bool                        IncludeBlackWinningEnding { get; set; }
            public  bool                        IncludeDrawEnding { get; set; }
        }
        
        public static StreamWriter? CreateOutFile(string outFileName) {
            StreamWriter? retVal;
            Stream        streamOut;
            
            try {
                streamOut = File.Create(outFileName);
                retVal    = new StreamWriter(streamOut, Encoding.GetEncoding("utf-8"));
            } catch(Exception) {
                MessageBox.Show($"Unable to create the file - {outFileName}");
                retVal = null;
            }
            return retVal;
        }

        private static void WritePgn(PgnLexical pgnBuffer, TextWriter writer, PgnGame pgnGame) => writer.Write(pgnBuffer.GetStringAtPos(pgnGame.StartingPos, pgnGame.Length));

        private static void GetPgnGameInfo(PgnGame      rawGame,
                                           out string?  gameResult,
                                           out string?  gameDate) {
            if (rawGame.Attrs == null || !rawGame.Attrs.TryGetValue("Result", out gameResult)) {
                gameResult = null;
            }
            if (rawGame.Attrs == null || !rawGame.Attrs.TryGetValue("Date", out gameDate)) {
                gameDate = null;
            }
        }

        
        private static bool IsRetained(PgnGame rawGame, int avgElo, FilterClause filterClause) {
            bool retVal;

            if (avgElo == -1) {
                retVal = filterClause.IncludesUnrated;
            } else if (filterClause.IsAllRanges) {
                retVal = true;
            } else {
                avgElo = avgElo / 100 * 100;
                retVal = filterClause.HashRanges!.ContainsKey(avgElo);
            }
            if (retVal) {
                if (!filterClause.IncludeAllPlayers || !filterClause.IncludeAllEnding) {
                    GetPgnGameInfo(rawGame, out string? gameResult,out _);
                    if (!filterClause.IncludeAllPlayers) {
                        if (!filterClause.HashPlayerList!.ContainsKey(rawGame.BlackPlayerName ?? "") &&
                            !filterClause.HashPlayerList!.ContainsKey(rawGame.WhitePlayerName ?? "")) {
                            retVal = false;
                        }
                    }
                    if (retVal && !filterClause.IncludeAllEnding) {
                        if (gameResult == "1-0") {
                            retVal = filterClause.IncludeWhiteWinningEnding;
                        } else if (gameResult == "0-1") {
                            retVal = filterClause.IncludeBlackWinningEnding;
                        } else if (gameResult == "1/2-1/2") {
                            retVal = filterClause.IncludeDrawEnding;
                        } else {
                            retVal = false;
                        }
                    }
                }                
            }
            return retVal;
        }

        public static int FilterPgn(PgnParser pgnParser, List<PgnGame> rawGames, TextWriter? textWriter, FilterClause filterClause) {
            int retVal;
            int whiteElo;
            int blackElo;
            int avgElo;
            
            retVal = 0;
            try {
                foreach (PgnGame rawGame in rawGames) {
                    whiteElo = rawGame.WhiteElo;
                    blackElo = rawGame.BlackElo;
                    avgElo   = (whiteElo != -1 && blackElo != -1) ? (whiteElo + blackElo) / 2 : -1;
                    if (IsRetained(rawGame, avgElo, filterClause)) {
                        if (textWriter != null) {
                            WritePgn(pgnParser.PgnLexical!, textWriter, rawGame);
                        }
                        retVal++;
                    }
                }
                textWriter?.Flush();
            } catch(Exception exc) {
                MessageBox.Show($"Error writing in destination file.\r\n{exc.Message}");
                retVal = 0;
            }
            return retVal;
        }
    }
}
