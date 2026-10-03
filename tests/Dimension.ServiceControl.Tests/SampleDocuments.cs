namespace Dimension.ServiceControl.Tests
{
    /// <summary>
    /// Documents shaped the way ScdSvcCtl.exe writes them, following the schemas it
    /// exports. Element names, enumeration values and the optional elements match the
    /// XSDs, so these stand in for the utility without one being installed.
    /// </summary>
    internal static class SampleDocuments
    {
        public const string ServiceList = @"<?xml version='1.0' encoding='utf-8'?>
<ServiceList Version='2307' MinorVersion='0'>
  <ServiceSegment>Default</ServiceSegment>
  <DirectoryServices>ldap://directory</DirectoryServices>
  <Agents>
    <Agent>
      <Name>APPSERVER01</Name>
      <Description>Primary application server</Description>
      <Tag>blue</Tag>
      <Services>
        <Service>
          <Instance>215-1</Instance>
          <ManagementMode>Automated</ManagementMode>
          <ServiceState>Online</ServiceState>
          <Stale>false</Stale>
          <Action>Idle</Action>
          <Id>215</Id>
          <Type>Calculation service</Type>
          <Configuration><Id>7</Id><Name>Default</Name><Description>Standard set</Description></Configuration>
          <Nickname>calc-a</Nickname>
          <Processes><Process><Name>ScdCalc.exe</Name><Id>4120</Id></Process></Processes>
        </Service>
        <Service>
          <Instance>300-1</Instance>
          <ManagementMode>Manual</ManagementMode>
          <ServiceState>Inoperable</ServiceState>
          <Stale>true</Stale>
          <Action>Undisclosed</Action>
          <Id>300</Id>
          <Type>Report service</Type>
          <Configuration />
          <Nickname></Nickname>
        </Service>
      </Services>
    </Agent>
    <Agent>
      <Name>APPSERVER02</Name>
      <Services />
    </Agent>
  </Agents>
  <Warnings>
    <Logs><Log><Severity>Warning</Severity><Message>Agent APPSERVER02 did not answer.</Message></Log></Logs>
    <Validations><Message>Listing may be incomplete.</Message></Validations>
  </Warnings>
</ServiceList>";

        public const string ServiceListWithWrongVersion = @"<ServiceList Version='9999' MinorVersion='1'>
  <Agents />
</ServiceList>";

        public const string ServiceListWithUnexpectedError = @"<ServiceList Version='2307' MinorVersion='0'>
  <UnexpectedError>The service operations controller is not reachable.</UnexpectedError>
</ServiceList>";

        public const string AgentList = @"<AgentList Version='641' MinorVersion='0'>
  <RunningAgents>
    <Agent>
      <Name>APPSERVER01</Name>
      <AgentType>MainAgent</AgentType>
      <Description>Primary</Description>
      <Tag>blue</Tag>
      <LoadIndex>3</LoadIndex>
      <AvailableMemory>8192</AvailableMemory>
    </Agent>
    <Agent>
      <Name>APPSERVER02</Name>
      <AgentType>Agent</AgentType>
      <Description>Secondary</Description>
    </Agent>
  </RunningAgents>
</AgentList>";

        public const string QueueItemList = @"<QueueitemList Version='2201' MinorVersion='0'>
  <QueueItems>
    <QueueItem>
      <QueueId>q-100</QueueId>
      <ActionType>Start</ActionType>
      <ActionState>Waiting</ActionState>
      <Service>
        <Id>*</Id>
        <Name>Calculation service</Name>
        <Configuration><Id>*</Id></Configuration>
        <Nickname></Nickname>
      </Service>
      <ManagementMode>*</ManagementMode>
      <Elastic></Elastic>
      <QueuedAt>2026-09-29T10:15:00</QueuedAt>
      <RequestedAt>2026-09-29T10:14:30</RequestedAt>
      <Source>ServiceControlUtility</Source>
      <Tag></Tag>
      <Reason>ControllerBusy</Reason>
    </QueueItem>
    <QueueItem>
      <QueueId>q-101</QueueId>
      <ActionType>Move</ActionType>
      <ActionState>Processing</ActionState>
      <Service>
        <Id>215</Id>
        <Name>Calculation service</Name>
        <InstanceFrom>215-1</InstanceFrom>
        <InstanceTo>215-2</InstanceTo>
        <Configuration><Id>7</Id><Name>Default</Name></Configuration>
        <Nickname>calc-a</Nickname>
      </Service>
      <AgentFrom>APPSERVER01</AgentFrom>
      <AgentTo>APPSERVER02</AgentTo>
      <ManagementMode>Automated</ManagementMode>
      <QueuedAt>2026-09-29T10:16:00Z</QueuedAt>
      <RequestedAt>2026-09-29T10:16:00Z</RequestedAt>
      <Source>Rebalancing</Source>
      <Tag>blue</Tag>
      <Reason>RequestAddedToQueue</Reason>
    </QueueItem>
  </QueueItems>
</QueueitemList>";
    }
}
