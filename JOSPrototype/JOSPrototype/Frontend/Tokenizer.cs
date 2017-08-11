using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JOSPrototype.Frontend
{
    enum Symbol { S_LParen, S_RParen, S_LBrace, S_RBrace, S_LBracket, S_RBracket, S_Comma, S_Semicolon, S_Colon, S_Assign, 
        S_Type, S_Var, S_Num, S_Sin, S_Plus, S_Minus, S_Times, S_Divide,
        S_Equal, S_NotEqual, S_Less, S_Greater, S_EqualLess, S_EqualGreater,
        S_None,
        S_True, S_False, S_AND, S_OR, S_NOT,
        S_BAND, S_BXOR,
        S_If, S_Else,
        S_Switch, S_Case, S_Default, S_Break,
        S_While,
        S_Return,
        S_EOF
    }
    class Token
    {
        public readonly Symbol sym;
        public readonly string sequence;

        public Token(Symbol sym, string sequence)
        {
            this.sym = sym;
            this.sequence = sequence;
        }
        public override string ToString()
        {
            return sym + ", " + sequence;
        }
    }
    class Tokenizer
    {
        public static List<Token> Tokenize(string str)
        {
            str = str.Trim();
            List<Token> tokens = new List<Token>();
            while (!str.Equals(""))
            {
                bool match = false;
                foreach(TokenInfo info in tokenInfos)
                {
                    Match m = info.regex.Match(str);
                    if (m.Success)
                    {
                        match = true;

                        string tok = m.Groups[1].Value;
                        tokens.Add(new Token(info.sym, tok));

                        str = info.regex.Replace(str, "", 1);
                        break;
                    }
                }
                if (!match)
                    throw new Exception("Unexpected character in input: " + str);
            }
            tokens.Add(new Token(Symbol.S_EOF, ""));
            return tokens;
        }

        static Tokenizer()
        {
            AddTokenInfo("System.Math.Sin", Symbol.S_Sin);
            AddTokenInfo("true", Symbol.S_True);
            AddTokenInfo("false", Symbol.S_False);
            AddTokenInfo("none", Symbol.S_None);
            AddTokenInfo("int|double|bool|CalcLong", Symbol.S_Type);
            AddTokenInfo("if", Symbol.S_If);
            AddTokenInfo("else", Symbol.S_Else);
            AddTokenInfo("switch", Symbol.S_Switch);
            AddTokenInfo("case", Symbol.S_Case);
            AddTokenInfo("default", Symbol.S_Default);
            AddTokenInfo("break", Symbol.S_Break);
            AddTokenInfo("while", Symbol.S_While);
            AddTokenInfo("return", Symbol.S_Return);      
            AddTokenInfo("[0-9]+(" + Regex.Escape(".") + "[0-9]+)?", Symbol.S_Num);
            AddTokenInfo(@"[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)*", Symbol.S_Var);
            AddTokenInfo(Regex.Escape("("), Symbol.S_LParen);
            AddTokenInfo(Regex.Escape(")"), Symbol.S_RParen);
            AddTokenInfo(Regex.Escape("{"), Symbol.S_LBrace);
            AddTokenInfo(Regex.Escape("}"), Symbol.S_RBrace);
            AddTokenInfo(Regex.Escape("["), Symbol.S_LBracket);
            AddTokenInfo(Regex.Escape("]"), Symbol.S_RBracket);
            AddTokenInfo(Regex.Escape(","), Symbol.S_Comma);
            AddTokenInfo(Regex.Escape(";"), Symbol.S_Semicolon);
            AddTokenInfo(Regex.Escape(":"), Symbol.S_Colon);            
            AddTokenInfo(Regex.Escape("+"), Symbol.S_Plus);
            AddTokenInfo(Regex.Escape("-"), Symbol.S_Minus);
            AddTokenInfo(Regex.Escape("*"), Symbol.S_Times);
            AddTokenInfo(Regex.Escape("/"), Symbol.S_Divide);
            AddTokenInfo(Regex.Escape("=="), Symbol.S_Equal);
            AddTokenInfo(Regex.Escape("!="), Symbol.S_NotEqual);
            AddTokenInfo(Regex.Escape("<="), Symbol.S_EqualLess);
            AddTokenInfo(Regex.Escape(">="), Symbol.S_EqualGreater);
            AddTokenInfo(Regex.Escape("<"), Symbol.S_Less);            
            AddTokenInfo(Regex.Escape(">"), Symbol.S_Greater);         
            AddTokenInfo(Regex.Escape("&&"), Symbol.S_AND);
            AddTokenInfo(Regex.Escape("||"), Symbol.S_OR);
            AddTokenInfo(Regex.Escape("&"), Symbol.S_BAND);
            AddTokenInfo(Regex.Escape("^"), Symbol.S_BXOR);
            AddTokenInfo(Regex.Escape("!"), Symbol.S_NOT);
            AddTokenInfo(Regex.Escape("="), Symbol.S_Assign);
        }
        private class TokenInfo
        {
            public readonly Regex regex;
            public readonly Symbol sym;

            public TokenInfo(Regex regex, Symbol sym)
            {
                this.regex = regex;
                this.sym = sym;
            }
        }
        private static List<TokenInfo> tokenInfos = new List<TokenInfo>();
        private static void AddTokenInfo(string regex, Symbol sym)
        {
            tokenInfos.Add(
            new TokenInfo(
            new Regex(@"^\s*(" + regex + @")", RegexOptions.Compiled), sym));
        }
    }
}
