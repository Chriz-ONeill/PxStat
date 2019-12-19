﻿using API;
using Pidgin;
using PxStat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace PxParser.Resources.Parser
{


    /// <summary>
    /// 
    /// </summary>
    public class PxParser
    {



        private static Parser<char, T> Tok<T>(Parser<char, T> token)
            => Try(token).Before(SkipWhitespaces);

        static readonly Parser<char, char> Quote = Char('"');
        static readonly Parser<char, char> Comma = Char(',');
        static readonly Parser<char, char> SemiColon = Char(';');
        static readonly Parser<char, char> CommaWhitespace = Comma.Between(SkipWhitespaces);

        static readonly Parser<char, char> TheKeyValueSeparator = Char('=');
        static readonly Parser<char, char> LBracket = Char('(');
        static readonly Parser<char, char> RBracket = Char(')');
        static readonly Parser<char, char> TokenSubkey = Token(c => c != '(' && c != ')');

        static readonly Parser<char, string> LineBreak = String("\r\n");
        static readonly Parser<char, Unit> SkipEndOfLine = LineBreak.SkipMany().Labelled("SkipEndOfLine");
        static readonly Parser<char, Unit> SkipWhiteSpaceEndOfLine = SkipWhitespaces.Then(SkipEndOfLine);
        static readonly Parser<char, char> SemiColonWhitespaceLineBreak = SemiColon.Between(SkipWhiteSpaceEndOfLine);

        static readonly Parser<char, string> SemiColonLineBreak = OneOf(Try(String(";\r\n")), String("\r\n;"));

        static readonly Parser<char, string> StringIdentifierBeforeSubkey = Token(c => c != '(' && c != '=').ManyString();
        static readonly Parser<char, string> StringIdentifierBeforeLanguage = Token(c => c != '[' && c != '=').ManyString();
        static readonly Parser<char, string> TheLanguage = Token(c => c != ']').ManyString();
        static readonly Parser<char, string> StringKey = Token(c => c != '=').ManyString();

        static readonly Parser<char, string> StringValue = Token(c => c != ',' && c != ';').ManyString();
        static readonly Parser<char, char> LSBracket = Char('[');
        static readonly Parser<char, char> RSBracket = Char(']');

        static readonly Parser<char, char> QuoteLinebreakQuote = Quote.Then(LineBreak).Then(Quote);

        // Different Value Types
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static double ConvertStringToDouble(string str)
        {
            return double.Parse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left">10</param>
        /// <param name="right">59</param>
        /// <returns></returns>
        public static double CreateFloat(long left, long right)
        {
            string sr = right.ToString();
            int n = sr.Length;
            double m = System.Math.Pow(10, n);
            double result = left + right * (1 / m);
            return result;
        }

        static readonly Parser<char, Maybe<char>> Sign = (Char('-').Or(Char('+'))).Optional();
        static readonly Parser<char, Maybe<char>> FloatingPointSeparator = Char('.').Optional();

        static readonly Parser<char, double> FloatingPoint =
            Map(
                (sign, left, separator, right)
                    => (sign.HasValue && sign.Value == '-'
                        ? -CreateFloat(left, (separator.HasValue && right.HasValue) ? right.Value : 0)
                        : CreateFloat(left, (separator.HasValue && right.HasValue) ? right.Value : 0)),
                Sign,
                UnsignedLong(10),
                FloatingPointSeparator,
                UnsignedLong(10).Optional()
            );

        static readonly Parser<char, IPxSingleElement> PxDoubleValue =
            FloatingPoint.Select<IPxSingleElement>(value => new PxDoubleValue(value)).Labelled("decimal literal");

        static readonly Parser<char, string> QuotedValue =
           Token(c => c != '"')
            .ManyString()
            .Between(Quote);

        static readonly Parser<char, string> QuotedValueMultiline =
            Token(c => c != '"')
            .ManyString().Before(SemiColon);

        static readonly Parser<char, string> QuotedDataValue =
            Token(c => c != '"')
            .ManyString()
            .Between(Quote);

        static readonly Parser<char, string> Confidential =
           String(Utility.GetCustomConfig("APP_PX_CONFIDENTIAL_VALUE"))
            .Between(Quote);

        static readonly Parser<char, IPxSingleElement> PxLiteralValue =
           Tok(Num)
            .Select<IPxSingleElement>(value => new PxLiteralValue(value))
            .Labelled("integer literal");

        static readonly Parser<char, IPxSingleElement> PxQuotedDataValue = QuotedDataValue.Select<IPxSingleElement>(s => new PxQuotedValue(s));

        static readonly Parser<char, IPxSingleElement> PxConfidential = Confidential.Select<IPxSingleElement>(s => new PxDotValue());

        static readonly Parser<char, IPxSingleElement> PxQuotedValue = QuotedValue.Select<IPxSingleElement>(s => new PxQuotedValue(s));

        static readonly Parser<char, IPxSingleElement> PxQuotedValueMultiline = QuotedValueMultiline.Select<IPxSingleElement>(s => new PxQuotedValueMultiline(s));

        static readonly Parser<char, IPxSingleElement> PxStringValue = StringValue.Select<IPxSingleElement>(val => new PxStringValue(val));

        static readonly Parser<char, IPxSingleElement> ASingleValue = Try(PxLiteralValue).Or(Try(PxQuotedValue)).Or(Try(PxStringValue));

        static readonly Parser<char, IPxSingleElement> ASingleValueMultiline = Try(PxLiteralValue).Or(Try(PxStringValue)).Or(Try(PxQuotedValueMultiline));

        static readonly Parser<char, IPxSingleElement> ADataValue = Try(PxDoubleValue).Or(Try(PxConfidential).Or(Try(PxQuotedDataValue)));// here

        static readonly Parser<char, string> ADataSeparator = OneOf(Try(Pidgin.Parser.String(" ").Then(LineBreak)), Try(Pidgin.Parser.String(" ")));

        static readonly Parser<char, IPxMultipleElements> ADataRepresentation =
            ADataValue.SeparatedAndOptionallyTerminated(ADataSeparator).Select<IPxMultipleElements>(values => new PxDataValue(new List<IPxSingleElement>(values)))
         .Before(SemiColonLineBreak);

        static readonly Parser<char, IPxMultipleElements> PxDataValue = LineBreak.Then(ADataRepresentation);

        /// <summary>
        /// Parses QuotedValue(s) separated by LineBreak ended by by 
        /// </summary>
        static readonly Parser<char, IPxMultipleElements> PxValueMultiline =
            QuotedValue.Separated(LineBreak).Select<IPxMultipleElements>(lines => new PxValueMultiline(new List<string>(lines))).Before(SemiColonLineBreak);

        /// <summary>
        /// Parses a Comma followed by a LineBreak or just a Comma
        /// </summary>
        static readonly Parser<char, string> CommaAndLineBreakOrJustComma = OneOf(Try(String(",").Then(LineBreak)), String(","));

        /// <summary>
        /// Parses at least a minimum of 2 ASingleValue comma separated, a line break can optionally follow the comma 
        /// </summary>
        static readonly Parser<char, IPxMultipleElements> PxListOfValues
            = ASingleValue.SeparatedAtLeastOnce(CommaAndLineBreakOrJustComma)
            .Select<IPxMultipleElements>(values => new PxListOfValues(new List<IPxSingleElement>(values)))
          .Before(SemiColonLineBreak);

        static readonly Parser<char, IPxMultipleElements> PxListOfValuesMultiline
            = ASingleValueMultiline.SeparatedAtLeastOnce(CommaAndLineBreakOrJustComma)
            .Select<IPxMultipleElements>(values => new PxListOfValues(new List<IPxSingleElement>(values)))
            .Before(SemiColonLineBreak);

        /// <summary>
        /// 
        /// </summary>
        static readonly Parser<char, PxSubKey> TheSubKey =
            Try(
                QuotedValue.Before(Comma).Then(QuotedValue, (s, v) => new PxSubKey(s, v))
                ).Or(
                    QuotedValue.Select(s => new PxSubKey(s))
                    );

        /// <summary>
        /// KEYWORD[LANGUAGE](SUB-KEY)
        /// </summary>
        static readonly Parser<char, PxKey> PxKeyWithIdentifierAndLanguageAndSubKey =
            Map(
                (id, lang, sub)
                    => new PxKey(
                            id,
                            lang,
                            sub
                        ),
                StringIdentifierBeforeLanguage,
                TheLanguage.Between(LSBracket, RSBracket),
                TheSubKey.Between(LBracket, RBracket)
            );


        /// <summary>
        /// KEYWORD[LANGUAGE]
        /// </summary>
        static readonly Parser<char, PxKey> PxKeyWithIdentifierAndLanguage =
            StringIdentifierBeforeLanguage
            .Then
            (
                TheLanguage.Between(LSBracket, RSBracket), (id, lang) => new PxKey(id, lang)
            );

        /// <summary>
        /// KEYWORD(SUB-KEY)
        /// </summary>
        static readonly Parser<char, PxKey> PxKeyWithIdentifierAndSubKey =
            StringIdentifierBeforeSubkey
            .Then
            (
                TheSubKey.Between(LBracket, RBracket), (id, subKey) => new PxKey(id, subKey)
            );

        /// <summary>
        /// KEYWORD
        /// </summary>
        static readonly Parser<char, PxKey> PxKeyJustWithIdentifier = StringKey.Select<PxKey>(id => new PxKey(id));

        /// <summary>
        /// KEYWORD[LANGUAGE](SUB-KEY)
        /// KEYWORD[LANGUAGE]
        /// KEYWORD(SUB-KEY)
        /// KEYWORD
        /// </summary>
        static readonly Parser<char, PxKey> PxKey =
            Try(PxKeyWithIdentifierAndLanguageAndSubKey)
            .Or(
                Try(PxKeyWithIdentifierAndLanguage)
                )
            .Or(
                Try(PxKeyWithIdentifierAndSubKey)
                )
            .Or(PxKeyJustWithIdentifier);

        /// <summary>
        /// 
        /// </summary>
        static readonly Parser<char, PxKey> TheKeyObject = PxKey.Before(TheKeyValueSeparator);

        /// <summary>
        /// 
        /// </summary>
        internal class PxValueParserFactory
        {

            internal static Parser<char, IPxElement> Create(PxKey key)
            {
                switch (key.Identifier)
                {
                    case "NOTEX":
                    case "NOTE":
                    case "TITLE":
                    case "CONTACT":
                    case "VALUENOTE":
                    case "VALUENOTEX":


                        return PxValueMultiline.Cast<IPxElement>();


                    //return PxListOfValuesMultiline.Cast<IPxElement>();
                    case "VALUES":
                    case "CODES":
                    case "HEADING":
                    case "LANGUAGES":
                    case "STUB":
                    case "TIMEVAL":
                        return PxListOfValues.Cast<IPxElement>();


                    case "DATA":
                        return PxDataValue.Cast<IPxElement>();

                    default:
                        return ASingleValue.Before(SemiColonLineBreak).Cast<IPxElement>();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static readonly Parser<char, IPxKeywordElement> TheKeywordObject =
            TheKeyObject
            .Then(key => PxValueParserFactory.Create(key), (key, val) => PxKeywordFactory.Create(key, val));

        /// <summary>
        /// 
        /// </summary>
        static readonly Parser<char, PxDocument> PxDocument = TheKeywordObject.Until(End).Select<PxDocument>(els => new PxDocument(new List<IPxKeywordElement>(els)));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static PxDocument Parse(string input)
        {

            string pxContent = input.TrimEnd(' ', '\r', '\n', '\t').TrimStart(' ', '\r', '\n', '\t');

            StringBuilder builder = new StringBuilder(pxContent);

            if (!pxContent.EndsWith(";"))
            {
                builder.Append(";");
                Log.Instance.Debug("Added ; to the end of px file");
            }

            builder.Append("\r\n");
            var parsedDocument = PxDocument.Parse(builder.ToString());
            if (!parsedDocument.Success)
            {
                ThrowParserException(parsedDocument.Error);
            }

            var val = parsedDocument.Value;

            return val;

            //return parsedDocument.Value;
            // return PxDocument.ParseOrThrow(builder.ToString());
        }



        /// <summary>
        /// Throws a parser exception. The Label.Get directives enable the message to be in the preferred language if available
        /// </summary>
        /// <param name="error"></param>
        internal static void ThrowParserException(ParseError<char> error)
        {
            StringBuilder sb = new StringBuilder();
            string unexpected = "";
            if (error.EOF) unexpected = Label.Get("error.parsing.eof");


            sb.AppendLine(Label.Get("error.parsing.parse-error"));

            if (error.Unexpected != null)
            {

                sb.AppendLine(string.Format(Label.Get("error.parsing.unexpected"), unexpected, error.ErrorPos.Line, error.ErrorPos.Col));
            }
            if (error.Expected != null)
            {
                sb.AppendLine(string.Format(Label.Get("error.parsing.expected"), GetExpected(error)));
            }

            throw new Exception(sb.ToString());
        }

        /// <summary>
        /// Iterates through the "Expected" parse error structure and retrieves expected values
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private static string GetExpected(ParseError<char> error)
        {
            string outstring = "";
            IEnumerable<Pidgin.Expected<char>> expected = error.Expected;
            foreach (var exp in expected)
            {
                foreach (var token in exp.Tokens)
                {
                    outstring = outstring + token.ToString();
                }
            }

            return outstring;
        }

        /// <summary>
        /// 
        /// </summary>
        private class PxKeywordFactory
        {
            internal static IPxKeywordElement Create(PxKey key, IPxElement val)
            {
                return new PxKeywordElement(key, val);
            }
        }
    }


}