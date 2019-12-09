// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: parliament_auth_contract.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace AElf.Client.ParliamentAuth {

  /// <summary>Holder for reflection information generated from parliament_auth_contract.proto</summary>
  public static partial class ParliamentAuthContractReflection {

    #region Descriptor
    /// <summary>File descriptor for parliament_auth_contract.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static ParliamentAuthContractReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Ch5wYXJsaWFtZW50X2F1dGhfY29udHJhY3QucHJvdG8aDGNsaWVudC5wcm90",
            "byI0Cg5Qcm9wb3NhbElkTGlzdBIiCgxwcm9wb3NhbF9pZHMYASADKAsyDC5j",
            "bGllbnQuSGFzaCKBAQoMT3JnYW5pemF0aW9uEhkKEXJlbGVhc2VfdGhyZXNo",
            "b2xkGAEgASgREi0KFG9yZ2FuaXphdGlvbl9hZGRyZXNzGAIgASgLMg8uY2xp",
            "ZW50LkFkZHJlc3MSJwoRb3JnYW5pemF0aW9uX2hhc2gYAyABKAsyDC5jbGll",
            "bnQuSGFzaEIdqgIaQUVsZi5DbGllbnQuUGFybGlhbWVudEF1dGhiBnByb3Rv",
            "Mw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::AElf.Client.Proto.ClientReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::AElf.Client.ParliamentAuth.ProposalIdList), global::AElf.Client.ParliamentAuth.ProposalIdList.Parser, new[]{ "ProposalIds" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::AElf.Client.ParliamentAuth.Organization), global::AElf.Client.ParliamentAuth.Organization.Parser, new[]{ "ReleaseThreshold", "OrganizationAddress", "OrganizationHash" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  ///parliament_auth
  /// </summary>
  public sealed partial class ProposalIdList : pb::IMessage<ProposalIdList> {
    private static readonly pb::MessageParser<ProposalIdList> _parser = new pb::MessageParser<ProposalIdList>(() => new ProposalIdList());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ProposalIdList> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::AElf.Client.ParliamentAuth.ParliamentAuthContractReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ProposalIdList() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ProposalIdList(ProposalIdList other) : this() {
      proposalIds_ = other.proposalIds_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ProposalIdList Clone() {
      return new ProposalIdList(this);
    }

    /// <summary>Field number for the "proposal_ids" field.</summary>
    public const int ProposalIdsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::AElf.Client.Proto.Hash> _repeated_proposalIds_codec
        = pb::FieldCodec.ForMessage(10, global::AElf.Client.Proto.Hash.Parser);
    private readonly pbc::RepeatedField<global::AElf.Client.Proto.Hash> proposalIds_ = new pbc::RepeatedField<global::AElf.Client.Proto.Hash>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::AElf.Client.Proto.Hash> ProposalIds {
      get { return proposalIds_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ProposalIdList);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ProposalIdList other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!proposalIds_.Equals(other.proposalIds_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= proposalIds_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      proposalIds_.WriteTo(output, _repeated_proposalIds_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += proposalIds_.CalculateSize(_repeated_proposalIds_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ProposalIdList other) {
      if (other == null) {
        return;
      }
      proposalIds_.Add(other.proposalIds_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            proposalIds_.AddEntriesFrom(input, _repeated_proposalIds_codec);
            break;
          }
        }
      }
    }

  }

  public sealed partial class Organization : pb::IMessage<Organization> {
    private static readonly pb::MessageParser<Organization> _parser = new pb::MessageParser<Organization>(() => new Organization());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<Organization> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::AElf.Client.ParliamentAuth.ParliamentAuthContractReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Organization() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Organization(Organization other) : this() {
      releaseThreshold_ = other.releaseThreshold_;
      organizationAddress_ = other.organizationAddress_ != null ? other.organizationAddress_.Clone() : null;
      organizationHash_ = other.organizationHash_ != null ? other.organizationHash_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Organization Clone() {
      return new Organization(this);
    }

    /// <summary>Field number for the "release_threshold" field.</summary>
    public const int ReleaseThresholdFieldNumber = 1;
    private int releaseThreshold_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int ReleaseThreshold {
      get { return releaseThreshold_; }
      set {
        releaseThreshold_ = value;
      }
    }

    /// <summary>Field number for the "organization_address" field.</summary>
    public const int OrganizationAddressFieldNumber = 2;
    private global::AElf.Client.Proto.Address organizationAddress_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::AElf.Client.Proto.Address OrganizationAddress {
      get { return organizationAddress_; }
      set {
        organizationAddress_ = value;
      }
    }

    /// <summary>Field number for the "organization_hash" field.</summary>
    public const int OrganizationHashFieldNumber = 3;
    private global::AElf.Client.Proto.Hash organizationHash_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::AElf.Client.Proto.Hash OrganizationHash {
      get { return organizationHash_; }
      set {
        organizationHash_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as Organization);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(Organization other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ReleaseThreshold != other.ReleaseThreshold) return false;
      if (!object.Equals(OrganizationAddress, other.OrganizationAddress)) return false;
      if (!object.Equals(OrganizationHash, other.OrganizationHash)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (ReleaseThreshold != 0) hash ^= ReleaseThreshold.GetHashCode();
      if (organizationAddress_ != null) hash ^= OrganizationAddress.GetHashCode();
      if (organizationHash_ != null) hash ^= OrganizationHash.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (ReleaseThreshold != 0) {
        output.WriteRawTag(8);
        output.WriteSInt32(ReleaseThreshold);
      }
      if (organizationAddress_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(OrganizationAddress);
      }
      if (organizationHash_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(OrganizationHash);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (ReleaseThreshold != 0) {
        size += 1 + pb::CodedOutputStream.ComputeSInt32Size(ReleaseThreshold);
      }
      if (organizationAddress_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(OrganizationAddress);
      }
      if (organizationHash_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(OrganizationHash);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(Organization other) {
      if (other == null) {
        return;
      }
      if (other.ReleaseThreshold != 0) {
        ReleaseThreshold = other.ReleaseThreshold;
      }
      if (other.organizationAddress_ != null) {
        if (organizationAddress_ == null) {
          OrganizationAddress = new global::AElf.Client.Proto.Address();
        }
        OrganizationAddress.MergeFrom(other.OrganizationAddress);
      }
      if (other.organizationHash_ != null) {
        if (organizationHash_ == null) {
          OrganizationHash = new global::AElf.Client.Proto.Hash();
        }
        OrganizationHash.MergeFrom(other.OrganizationHash);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            ReleaseThreshold = input.ReadSInt32();
            break;
          }
          case 18: {
            if (organizationAddress_ == null) {
              OrganizationAddress = new global::AElf.Client.Proto.Address();
            }
            input.ReadMessage(OrganizationAddress);
            break;
          }
          case 26: {
            if (organizationHash_ == null) {
              OrganizationHash = new global::AElf.Client.Proto.Hash();
            }
            input.ReadMessage(OrganizationHash);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code