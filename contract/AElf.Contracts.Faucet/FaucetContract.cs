using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Faucet
{
    public partial class FaucetContract : FaucetContractContainer.FaucetContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            var nativeSymbol = Context.Variables.NativeSymbol;
            State.OwnerMap[nativeSymbol] = input.Admin ?? Context.Sender;
            State.AmountLimitMap[nativeSymbol] = input.AmountLimit == 0 ? DefaultLimitAmount : input.AmountLimit;
            State.IntervalMinutesMap[nativeSymbol] =
                input.IntervalMinutes == 0 ? DefaultIntervalMinutes : input.IntervalMinutes;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }

        public override Empty TurnOn(TurnInput input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
            AssertSenderIsOwner(symbol);
            State.OffAtMap.Remove(symbol);
            State.OnAtMap[symbol] = input.At == null ? Context.CurrentBlockTime : input.At;
            Context.Fire(new FaucetTurned
            {
                IsTurnedOn = true,
                Symbol = symbol
            });
            return new Empty();
        }

        public override Empty TurnOff(TurnInput input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
            AssertSenderIsOwner(symbol);
            State.OnAtMap.Remove(symbol);
            State.OffAtMap[symbol] = input.At == null ? Context.CurrentBlockTime : input.At;
            Context.Fire(new FaucetTurned
            {
                IsTurnedOn = false,
                Symbol = symbol
            });
            return new Empty();
        }

        public override Empty NewFaucet(NewFaucetInput input)
        {
            AssertSenderIsAdmin();
            State.OwnerMap[input.Symbol] = input.Owner;
            State.AmountLimitMap[input.Symbol] = input.AmountLimit == 0 ? DefaultLimitAmount : input.AmountLimit;
            State.IntervalMinutesMap[input.Symbol] =
                input.IntervalMinutes == 0 ? DefaultIntervalMinutes : input.IntervalMinutes;
            Context.Fire(new FaucetCreated
            {
                Owner = input.Owner,
                Amount = input.Amount,
                Symbol = input.Symbol
            });
            return new Empty();
        }

        public override Empty Pour(PourInput input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
            AssertSenderIsOwner(symbol);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = symbol,
                Amount = input.Amount
            });
            return new Empty();
        }

        public override Empty SetLimit(SetLimitInput input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
            AssertSenderIsOwner(symbol);
            State.AmountLimitMap[symbol] = input.AmountLimit;
            State.IntervalMinutesMap[symbol] = input.IntervalMinutes;
            return new Empty();
        }

        public override Empty Ban(BanInput input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
            AssertSenderIsOwner(symbol);
            if (input.IsBan)
            {
                State.BanMap[symbol][input.Target] = true;
            }
            else
            {
                State.BanMap[symbol].Remove(input.Target);
            }

            return new Empty();
        }

        public override Empty Take(TakeInput input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
            AssertFaucetIsOn(symbol);
            Assert(State.BanMap[symbol][Context.Sender] == false, $"Sender is banned by faucet owner of {symbol}");
            var latestTakeTime = State.LatestTakeTimeMap[symbol][Context.Sender];
            if (latestTakeTime != null)
            {
                var nextAvailableTime = latestTakeTime.AddMinutes(State.IntervalMinutesMap[symbol]);
                Assert(Context.CurrentBlockTime >= nextAvailableTime,
                    $"Can take {symbol} again after {nextAvailableTime}");
            }

            var amount = Math.Max(State.AmountLimitMap[symbol], input.Amount);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = symbol,
                Amount = amount,
                To = Context.Sender
            });
            return new Empty();
        }

        public override Empty Return(ReturnInput input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Symbol);
            var amount = input.Amount;
            if (input.Amount == 0)
            {
                amount = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = Context.Sender,
                    Symbol = symbol
                }).Balance;
            }

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = amount,
                Symbol = symbol
            });
            return new Empty();
        }

        public override Address GetOwner(StringValue input)
        {
            return State.OwnerMap[ReturnNativeSymbolIfEmpty(input.Value)];
        }

        public override FaucetStatus GetFaucetStatus(StringValue input)
        {
            var symbol = ReturnNativeSymbolIfEmpty(input.Value);

            var maybeOnAt = State.OnAtMap[symbol];
            var isOn = maybeOnAt != null;
            var status = new FaucetStatus
            {
                IsOn = isOn,
                TurnAt = isOn ? maybeOnAt : State.OffAtMap[symbol]
            };

            return status;
        }

        private void AssertSenderIsOwner(string symbol)
        {
            Assert(Context.Sender == State.OwnerMap[symbol], $"No permission to operate faucet of {symbol}.");
        }

        private void AssertSenderIsAdmin()
        {
            Assert(Context.Sender == State.OwnerMap[Context.Variables.NativeSymbol], "No permission.");
        }

        private void AssertFaucetIsOff(string symbol)
        {
            Assert(State.OnAtMap[symbol] == null, $"Faucet of {symbol} is on.");
        }

        private void AssertFaucetIsOn(string symbol)
        {
            Assert(State.OffAtMap[symbol] == null, $"Faucet of {symbol} is off.");
        }

        private string ReturnNativeSymbolIfEmpty(string symbol)
        {
            return string.IsNullOrEmpty(symbol) ? Context.Variables.NativeSymbol : symbol;
        }
    }
}