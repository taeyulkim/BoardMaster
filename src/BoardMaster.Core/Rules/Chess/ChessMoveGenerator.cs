namespace BoardMaster.Core.Rules.Chess
{
    using Ont = BoardMaster.Core.Ontology;

    /// <summary>
    /// 체스 합법수 생성/검증을 전담하는 순수 계산 엔진입니다. GameContext를 읽기만 하다가, 자기 King이
    /// 위험해지는지 확인할 때만 기물을 잠깐 실제로 옮겨보고(Zone 참조를 바꿨다가 finally에서 되돌림)
    /// 곧바로 원상복구합니다 — 체스는 기물이 최대 32개뿐이라 Go의 GoGroupScanner 같은 zero-alloc
    /// 설계까지는 필요 없다고 판단했습니다(MCTS 같은 초당 수만 회 호출 경로가 없습니다).
    ///
    /// 용어: "pseudo-legal"은 자기 King이 체크에 노출되는지는 아직 안 따진 후보 수, "legal"은 그것까지
    /// 걸러낸 진짜 합법수입니다.
    /// </summary>
    internal static class ChessMoveGenerator
    {
        private static readonly (int Dx, int Dy)[] s_a_stKnightOffsets =
        {
            (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)
        };

        private static readonly (int Dx, int Dy)[] s_a_stBishopDirections = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        private static readonly (int Dx, int Dy)[] s_a_stRookDirections = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        private static readonly (int Dx, int Dy)[] s_a_stKingOffsets =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)
        };

        // ----- 공개 API -----

        public static bool IsInCheck(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor)
        {
            Ont.Entity objKing = ChessZoneQuery.FindKing(p_objContext, p_eColor);
            Ont.E_PlayerColor eOpponent = Opponent(p_eColor);
            return IsSquareAttacked(p_objContext, objKing.mv_objLocatedZone.mv_nX, objKing.mv_objLocatedZone.mv_nY, eOpponent);
        }

        public static List<ChessMove> GetLegalMoves(
            Ont.GameContext p_objContext, int p_nFromX, int p_nFromY, ChessCastlingRights p_stRights, (int X, int Y)? p_stEnPassantTarget)
        {
            List<ChessMove> lisLegal = new List<ChessMove>();
            Ont.Entity? objPiece = ChessZoneQuery.FindPieceAt(p_objContext, p_nFromX, p_nFromY);
            if (objPiece is null)
            {
                return lisLegal;
            }

            foreach (ChessMove stCandidate in GetPseudoLegalMoves(p_objContext, objPiece, p_nFromX, p_nFromY, p_stRights, p_stEnPassantTarget))
            {
                if (!WouldExposeOwnKing(p_objContext, objPiece, p_nFromX, p_nFromY, stCandidate))
                {
                    lisLegal.Add(stCandidate);
                }
            }

            return lisLegal;
        }

        public static bool IsLegalMove(
            Ont.GameContext p_objContext, int p_nFromX, int p_nFromY, int p_nToX, int p_nToY,
            ChessCastlingRights p_stRights, (int X, int Y)? p_stEnPassantTarget)
        {
            foreach (ChessMove stMove in GetLegalMoves(p_objContext, p_nFromX, p_nFromY, p_stRights, p_stEnPassantTarget))
            {
                if (stMove.ToX == p_nToX && stMove.ToY == p_nToY)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAnyLegalMove(
            Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, ChessCastlingRights p_stRights, (int X, int Y)? p_stEnPassantTarget)
        {
            foreach (Ont.Entity objPiece in ChessZoneQuery.FindActivePieces(p_objContext, p_eColor))
            {
                int nX = objPiece.mv_objLocatedZone.mv_nX;
                int nY = objPiece.mv_objLocatedZone.mv_nY;
                if (GetLegalMoves(p_objContext, nX, nY, p_stRights, p_stEnPassantTarget).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        // ----- 피격 판정 -----

        public static bool IsSquareAttacked(Ont.GameContext p_objContext, int p_nX, int p_nY, Ont.E_PlayerColor p_eByColor)
        {
            foreach (Ont.Entity objPiece in ChessZoneQuery.FindActivePieces(p_objContext, p_eByColor))
            {
                int nPieceX = objPiece.mv_objLocatedZone.mv_nX;
                int nPieceY = objPiece.mv_objLocatedZone.mv_nY;

                switch (objPiece.mv_strType)
                {
                    case ChessPieceType.Pawn:
                        int nDir = p_eByColor == Ont.E_PlayerColor.White ? 1 : -1;
                        if (nPieceY + nDir == p_nY && Math.Abs(nPieceX - p_nX) == 1)
                        {
                            return true;
                        }

                        break;

                    case ChessPieceType.Knight:
                        foreach ((int Dx, int Dy) in s_a_stKnightOffsets)
                        {
                            if (nPieceX + Dx == p_nX && nPieceY + Dy == p_nY)
                            {
                                return true;
                            }
                        }

                        break;

                    case ChessPieceType.King:
                        if (Math.Abs(nPieceX - p_nX) <= 1 && Math.Abs(nPieceY - p_nY) <= 1 && (nPieceX != p_nX || nPieceY != p_nY))
                        {
                            return true;
                        }

                        break;

                    case ChessPieceType.Bishop:
                        if (SlidesTo(p_objContext, nPieceX, nPieceY, p_nX, p_nY, s_a_stBishopDirections))
                        {
                            return true;
                        }

                        break;

                    case ChessPieceType.Rook:
                        if (SlidesTo(p_objContext, nPieceX, nPieceY, p_nX, p_nY, s_a_stRookDirections))
                        {
                            return true;
                        }

                        break;

                    case ChessPieceType.Queen:
                        if (SlidesTo(p_objContext, nPieceX, nPieceY, p_nX, p_nY, s_a_stBishopDirections)
                            || SlidesTo(p_objContext, nPieceX, nPieceY, p_nX, p_nY, s_a_stRookDirections))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        /// <summary>(p_nFromX, p_nFromY)의 슬라이딩 기물이 방해받지 않고 (p_nToX, p_nToY)까지 닿는지.</summary>
        private static bool SlidesTo(Ont.GameContext p_objContext, int p_nFromX, int p_nFromY, int p_nToX, int p_nToY, (int Dx, int Dy)[] p_a_stDirections)
        {
            foreach ((int Dx, int Dy) in p_a_stDirections)
            {
                int nX = p_nFromX + Dx;
                int nY = p_nFromY + Dy;

                while (InBounds(nX, nY))
                {
                    if (nX == p_nToX && nY == p_nToY)
                    {
                        return true;
                    }

                    if (ChessZoneQuery.FindPieceAt(p_objContext, nX, nY) is not null)
                    {
                        break; // 뭔가에 막혀서 더 못 감 — 그 칸까지만 닿고 그 뒤는 못 간다.
                    }

                    nX += Dx;
                    nY += Dy;
                }
            }

            return false;
        }

        // ----- 기물별 pseudo-legal 후보 생성 -----

        private static List<ChessMove> GetPseudoLegalMoves(
            Ont.GameContext p_objContext, Ont.Entity p_objPiece, int p_nX, int p_nY,
            ChessCastlingRights p_stRights, (int X, int Y)? p_stEnPassantTarget)
        {
            return p_objPiece.mv_strType switch
            {
                ChessPieceType.Pawn => GetPawnMoves(p_objContext, p_objPiece.mv_eColor, p_nX, p_nY, p_stEnPassantTarget),
                ChessPieceType.Knight => GetSteppingMoves(p_objContext, p_objPiece.mv_eColor, p_nX, p_nY, s_a_stKnightOffsets),
                ChessPieceType.Bishop => GetSlidingMoves(p_objContext, p_objPiece.mv_eColor, p_nX, p_nY, s_a_stBishopDirections),
                ChessPieceType.Rook => GetSlidingMoves(p_objContext, p_objPiece.mv_eColor, p_nX, p_nY, s_a_stRookDirections),
                ChessPieceType.Queen => CombineSliding(p_objContext, p_objPiece.mv_eColor, p_nX, p_nY),
                ChessPieceType.King => GetKingMoves(p_objContext, p_objPiece.mv_eColor, p_nX, p_nY, p_stRights),
                _ => new List<ChessMove>()
            };
        }

        private static List<ChessMove> GetPawnMoves(
            Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nX, int p_nY, (int X, int Y)? p_stEnPassantTarget)
        {
            List<ChessMove> lisMoves = new List<ChessMove>();
            int nDir = p_eColor == Ont.E_PlayerColor.White ? 1 : -1;
            int nStartRank = p_eColor == Ont.E_PlayerColor.White ? 1 : 6;
            int nPromotionRank = p_eColor == Ont.E_PlayerColor.White ? 7 : 0;

            int nOneStepY = p_nY + nDir;
            if (InBounds(p_nX, nOneStepY) && ChessZoneQuery.FindPieceAt(p_objContext, p_nX, nOneStepY) is null)
            {
                lisMoves.Add(new ChessMove(p_nX, nOneStepY, false, false, false, false, nOneStepY == nPromotionRank));

                int nTwoStepY = p_nY + (2 * nDir);
                if (p_nY == nStartRank && ChessZoneQuery.FindPieceAt(p_objContext, p_nX, nTwoStepY) is null)
                {
                    lisMoves.Add(new ChessMove(p_nX, nTwoStepY, false, false, false, false, false));
                }
            }

            foreach (int nDx in new[] { -1, 1 })
            {
                int nCaptureX = p_nX + nDx;
                int nCaptureY = p_nY + nDir;
                if (!InBounds(nCaptureX, nCaptureY))
                {
                    continue;
                }

                Ont.Entity? objTarget = ChessZoneQuery.FindPieceAt(p_objContext, nCaptureX, nCaptureY);
                if (objTarget is not null && objTarget.mv_eColor != p_eColor)
                {
                    lisMoves.Add(new ChessMove(nCaptureX, nCaptureY, true, false, false, false, nCaptureY == nPromotionRank));
                }
                else if (objTarget is null && p_stEnPassantTarget is { } stTarget && stTarget.X == nCaptureX && stTarget.Y == nCaptureY)
                {
                    lisMoves.Add(new ChessMove(nCaptureX, nCaptureY, true, true, false, false, false));
                }
            }

            return lisMoves;
        }

        private static List<ChessMove> GetSteppingMoves(
            Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nX, int p_nY, (int Dx, int Dy)[] p_a_stOffsets)
        {
            List<ChessMove> lisMoves = new List<ChessMove>();

            foreach ((int Dx, int Dy) in p_a_stOffsets)
            {
                int nX = p_nX + Dx;
                int nY = p_nY + Dy;
                if (!InBounds(nX, nY))
                {
                    continue;
                }

                Ont.Entity? objTarget = ChessZoneQuery.FindPieceAt(p_objContext, nX, nY);
                if (objTarget is null)
                {
                    lisMoves.Add(new ChessMove(nX, nY, false, false, false, false, false));
                }
                else if (objTarget.mv_eColor != p_eColor)
                {
                    lisMoves.Add(new ChessMove(nX, nY, true, false, false, false, false));
                }
            }

            return lisMoves;
        }

        private static List<ChessMove> GetSlidingMoves(
            Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nX, int p_nY, (int Dx, int Dy)[] p_a_stDirections)
        {
            List<ChessMove> lisMoves = new List<ChessMove>();

            foreach ((int Dx, int Dy) in p_a_stDirections)
            {
                int nX = p_nX + Dx;
                int nY = p_nY + Dy;

                while (InBounds(nX, nY))
                {
                    Ont.Entity? objTarget = ChessZoneQuery.FindPieceAt(p_objContext, nX, nY);
                    if (objTarget is null)
                    {
                        lisMoves.Add(new ChessMove(nX, nY, false, false, false, false, false));
                    }
                    else
                    {
                        if (objTarget.mv_eColor != p_eColor)
                        {
                            lisMoves.Add(new ChessMove(nX, nY, true, false, false, false, false));
                        }

                        break; // 자기 편이든 상대편이든, 뭔가에 막히면 그 이상은 못 간다.
                    }

                    nX += Dx;
                    nY += Dy;
                }
            }

            return lisMoves;
        }

        private static List<ChessMove> CombineSliding(Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nX, int p_nY)
        {
            List<ChessMove> lisMoves = GetSlidingMoves(p_objContext, p_eColor, p_nX, p_nY, s_a_stBishopDirections);
            lisMoves.AddRange(GetSlidingMoves(p_objContext, p_eColor, p_nX, p_nY, s_a_stRookDirections));
            return lisMoves;
        }

        private static List<ChessMove> GetKingMoves(
            Ont.GameContext p_objContext, Ont.E_PlayerColor p_eColor, int p_nX, int p_nY, ChessCastlingRights p_stRights)
        {
            List<ChessMove> lisMoves = GetSteppingMoves(p_objContext, p_eColor, p_nX, p_nY, s_a_stKingOffsets);

            bool bKingMoved = p_eColor == Ont.E_PlayerColor.White ? p_stRights.WhiteKingMoved : p_stRights.BlackKingMoved;
            if (bKingMoved)
            {
                return lisMoves; // King이 한 번이라도 움직였으면 이 색은 캐슬링이 영구히 불가능하다.
            }

            Ont.E_PlayerColor eOpponent = Opponent(p_eColor);
            if (IsSquareAttacked(p_objContext, p_nX, p_nY, eOpponent))
            {
                return lisMoves; // 체크 상태에서는 캐슬링할 수 없다.
            }

            int nHomeRank = p_nY;
            bool bKingsideRookMoved = p_eColor == Ont.E_PlayerColor.White ? p_stRights.WhiteKingsideRookMoved : p_stRights.BlackKingsideRookMoved;
            if (!bKingsideRookMoved
                && ChessZoneQuery.FindPieceAt(p_objContext, 5, nHomeRank) is null
                && ChessZoneQuery.FindPieceAt(p_objContext, 6, nHomeRank) is null
                && ChessZoneQuery.FindPieceAt(p_objContext, 7, nHomeRank) is { } objKingsideRook
                && objKingsideRook.mv_strType == ChessPieceType.Rook && objKingsideRook.mv_eColor == p_eColor
                && !IsSquareAttacked(p_objContext, 5, nHomeRank, eOpponent)
                && !IsSquareAttacked(p_objContext, 6, nHomeRank, eOpponent))
            {
                lisMoves.Add(new ChessMove(6, nHomeRank, false, false, true, false, false));
            }

            bool bQueensideRookMoved = p_eColor == Ont.E_PlayerColor.White ? p_stRights.WhiteQueensideRookMoved : p_stRights.BlackQueensideRookMoved;
            if (!bQueensideRookMoved
                && ChessZoneQuery.FindPieceAt(p_objContext, 1, nHomeRank) is null
                && ChessZoneQuery.FindPieceAt(p_objContext, 2, nHomeRank) is null
                && ChessZoneQuery.FindPieceAt(p_objContext, 3, nHomeRank) is null
                && ChessZoneQuery.FindPieceAt(p_objContext, 0, nHomeRank) is { } objQueensideRook
                && objQueensideRook.mv_strType == ChessPieceType.Rook && objQueensideRook.mv_eColor == p_eColor
                && !IsSquareAttacked(p_objContext, 3, nHomeRank, eOpponent)
                && !IsSquareAttacked(p_objContext, 2, nHomeRank, eOpponent))
            {
                lisMoves.Add(new ChessMove(2, nHomeRank, false, false, false, true, false));
            }

            return lisMoves;
        }

        // ----- 자충(자기 King 노출) 시뮬레이션 -----

        private static bool WouldExposeOwnKing(Ont.GameContext p_objContext, Ont.Entity p_objMovingPiece, int p_nFromX, int p_nFromY, ChessMove p_stMove)
        {
            Ont.Zone objOriginalZone = p_objMovingPiece.mv_objLocatedZone;
            Ont.Zone objDestinationZone = p_objContext.mv_dicZones[ChessZoneId.Square(p_stMove.ToX, p_stMove.ToY)];
            Ont.Zone objCapturedZone = p_objContext.mv_dicZones[ChessZoneId.Captured];

            Ont.Entity? objCapturedPiece = ChessZoneQuery.FindPieceAt(p_objContext, p_stMove.ToX, p_stMove.ToY);
            Ont.Zone? objCapturedOriginalZone = objCapturedPiece?.mv_objLocatedZone;

            Ont.Entity? objEnPassantVictim = null;
            Ont.Zone? objEnPassantVictimOriginalZone = null;
            if (p_stMove.IsEnPassantCapture)
            {
                objEnPassantVictim = ChessZoneQuery.FindPieceAt(p_objContext, p_stMove.ToX, p_nFromY);
                objEnPassantVictimOriginalZone = objEnPassantVictim?.mv_objLocatedZone;
            }

            p_objMovingPiece.mv_objLocatedZone = objDestinationZone;
            if (objCapturedPiece is not null)
            {
                objCapturedPiece.mv_objLocatedZone = objCapturedZone;
            }

            if (objEnPassantVictim is not null)
            {
                objEnPassantVictim.mv_objLocatedZone = objCapturedZone;
            }

            try
            {
                return IsInCheck(p_objContext, p_objMovingPiece.mv_eColor);
            }
            finally
            {
                p_objMovingPiece.mv_objLocatedZone = objOriginalZone;
                if (objCapturedPiece is not null)
                {
                    objCapturedPiece.mv_objLocatedZone = objCapturedOriginalZone!;
                }

                if (objEnPassantVictim is not null)
                {
                    objEnPassantVictim.mv_objLocatedZone = objEnPassantVictimOriginalZone!;
                }
            }
        }

        private static bool InBounds(int p_nX, int p_nY)
        {
            return p_nX >= 0 && p_nX < ChessGameFactory.BOARD_SIZE && p_nY >= 0 && p_nY < ChessGameFactory.BOARD_SIZE;
        }

        private static Ont.E_PlayerColor Opponent(Ont.E_PlayerColor p_eColor)
        {
            return p_eColor == Ont.E_PlayerColor.White ? Ont.E_PlayerColor.Black : Ont.E_PlayerColor.White;
        }
    }
}
