//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using Emul8.Logging;
using Emul8.UserInterface;

namespace Emul8.Peripherals.Network
{
    [Icon("phy")]
    public class EthernetPhysicalLayer : IPhysicalLayer<ushort>
    {
        public EthernetPhysicalLayer(ushort id1, ushort id2, ushort autoNegotiationAdvertisement,
            ushort autoNegotiationLinkPartnerAbility, ushort gigabitControl = 0, ushort gigabitStatus = 0)
        {
            this.Id1 = id1;
            this.Id2 = id2;
            this.AutoNegotiationAdvertisement = autoNegotiationAdvertisement;
            this.AutoNegotiationLinkPartnerAbility = autoNegotiationLinkPartnerAbility;
            this.GigabitControl = gigabitControl;
            this.GigabitStatus = gigabitStatus;
        }

        public ushort Read(ushort addr)
        {
            switch((Register)addr)
            {
            case Register.BasicStatus:
                return (ushort)(1u<<5 | 1u<<2); //link up, auto-negotiation complete
            case Register.Id1:
                return Id1;
            case Register.Id2:
                return Id2;
            case Register.AutoNegotiationAdvertisement:
                return AutoNegotiationAdvertisement;
            case Register.AutoNegotiationLinkPartnerAbility:
                return AutoNegotiationLinkPartnerAbility;
            case Register.GigabitControl:
                return GigabitControl;
            case Register.GigabitStatus:
                return GigabitStatus;
            default:
                this.LogUnhandledRead(addr);
                return 0;
            }
        }

        public void Write(ushort addr, ushort val)
        {
            this.LogUnhandledWrite(addr, val);
        }

        public void Reset()
        {
        }


        protected ushort Id1;
        protected ushort Id2;
        protected ushort AutoNegotiationAdvertisement;
        protected ushort AutoNegotiationLinkPartnerAbility;
        protected ushort GigabitControl;
        protected ushort GigabitStatus;


        protected enum Register
        {
            BasicControl = 0,
            BasicStatus = 1,
            Id1 = 2,
            Id2 = 3,
            AutoNegotiationAdvertisement = 4,
            AutoNegotiationLinkPartnerAbility = 5,
            GigabitControl = 9,
            GigabitStatus = 10
        }

    }
}
