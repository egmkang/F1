// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Message/rpc.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace F1.Message {

  /// <summary>Holder for reflection information generated from Message/rpc.proto</summary>
  public static partial class RpcReflection {

    #region Descriptor
    /// <summary>File descriptor for Message/rpc.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static RpcReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChFNZXNzYWdlL3JwYy5wcm90bxIKRjEuTWVzc2FnZSI0CgtSZXF1ZXN0UGlu",
            "ZxIRCglzZXJ2ZXJfaWQYASABKAUSEgoKc3RhcnRfdGltZRgCIAEoAyI1CgxS",
            "ZXNwb25zZVBvbmcSEQoJc2VydmVyX2lkGAEgASgFEhIKCnN0YXJ0X3RpbWUY",
            "AiABKAMigAEKClJlcXVlc3RScGMSEwoLZW50aXR5X3R5cGUYASABKAUSEQoJ",
            "ZW50aXR5X2lkGAIgASgJEg4KBm1ldGhvZBgDIAEoCRIMCgRhcmdzGAQgASgM",
            "EgwKBGhvc3QYBSABKAkSEgoKcmVxdWVzdF9pZBgGIAEoAxIKCgJpZBgHIAEo",
            "AyJNCgtSZXNwb25zZVJwYxIKCgJpZBgBIAEoAxISCgplcnJvcl9jb2RlGAIg",
            "ASgFEhEKCWVycm9yX21zZxgDIAEoCRILCgNyZXQYBSABKAwiKQoQUmVxdWVz",
            "dEhlYXJ0QmVhdBIVCg1taWxsaV9zZWNvbmRzGAEgASgDIioKEVJlc3BvbnNl",
            "SGVhcnRCZWF0EhUKDW1pbGxpX3NlY29uZHMYASABKANiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::F1.Message.RequestPing), global::F1.Message.RequestPing.Parser, new[]{ "ServerId", "StartTime" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::F1.Message.ResponsePong), global::F1.Message.ResponsePong.Parser, new[]{ "ServerId", "StartTime" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::F1.Message.RequestRpc), global::F1.Message.RequestRpc.Parser, new[]{ "EntityType", "EntityId", "Method", "Args", "Host", "RequestId", "Id" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::F1.Message.ResponseRpc), global::F1.Message.ResponseRpc.Parser, new[]{ "Id", "ErrorCode", "ErrorMsg", "Ret" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::F1.Message.RequestHeartBeat), global::F1.Message.RequestHeartBeat.Parser, new[]{ "MilliSeconds" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::F1.Message.ResponseHeartBeat), global::F1.Message.ResponseHeartBeat.Parser, new[]{ "MilliSeconds" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class RequestPing : pb::IMessage<RequestPing> {
    private static readonly pb::MessageParser<RequestPing> _parser = new pb::MessageParser<RequestPing>(() => new RequestPing());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<RequestPing> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::F1.Message.RpcReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestPing() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestPing(RequestPing other) : this() {
      serverId_ = other.serverId_;
      startTime_ = other.startTime_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestPing Clone() {
      return new RequestPing(this);
    }

    /// <summary>Field number for the "server_id" field.</summary>
    public const int ServerIdFieldNumber = 1;
    private int serverId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int ServerId {
      get { return serverId_; }
      set {
        serverId_ = value;
      }
    }

    /// <summary>Field number for the "start_time" field.</summary>
    public const int StartTimeFieldNumber = 2;
    private long startTime_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long StartTime {
      get { return startTime_; }
      set {
        startTime_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as RequestPing);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(RequestPing other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ServerId != other.ServerId) return false;
      if (StartTime != other.StartTime) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (ServerId != 0) hash ^= ServerId.GetHashCode();
      if (StartTime != 0L) hash ^= StartTime.GetHashCode();
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
      if (ServerId != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(ServerId);
      }
      if (StartTime != 0L) {
        output.WriteRawTag(16);
        output.WriteInt64(StartTime);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (ServerId != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(ServerId);
      }
      if (StartTime != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(StartTime);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(RequestPing other) {
      if (other == null) {
        return;
      }
      if (other.ServerId != 0) {
        ServerId = other.ServerId;
      }
      if (other.StartTime != 0L) {
        StartTime = other.StartTime;
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
            ServerId = input.ReadInt32();
            break;
          }
          case 16: {
            StartTime = input.ReadInt64();
            break;
          }
        }
      }
    }

  }

  public sealed partial class ResponsePong : pb::IMessage<ResponsePong> {
    private static readonly pb::MessageParser<ResponsePong> _parser = new pb::MessageParser<ResponsePong>(() => new ResponsePong());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ResponsePong> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::F1.Message.RpcReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponsePong() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponsePong(ResponsePong other) : this() {
      serverId_ = other.serverId_;
      startTime_ = other.startTime_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponsePong Clone() {
      return new ResponsePong(this);
    }

    /// <summary>Field number for the "server_id" field.</summary>
    public const int ServerIdFieldNumber = 1;
    private int serverId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int ServerId {
      get { return serverId_; }
      set {
        serverId_ = value;
      }
    }

    /// <summary>Field number for the "start_time" field.</summary>
    public const int StartTimeFieldNumber = 2;
    private long startTime_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long StartTime {
      get { return startTime_; }
      set {
        startTime_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ResponsePong);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ResponsePong other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ServerId != other.ServerId) return false;
      if (StartTime != other.StartTime) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (ServerId != 0) hash ^= ServerId.GetHashCode();
      if (StartTime != 0L) hash ^= StartTime.GetHashCode();
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
      if (ServerId != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(ServerId);
      }
      if (StartTime != 0L) {
        output.WriteRawTag(16);
        output.WriteInt64(StartTime);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (ServerId != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(ServerId);
      }
      if (StartTime != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(StartTime);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ResponsePong other) {
      if (other == null) {
        return;
      }
      if (other.ServerId != 0) {
        ServerId = other.ServerId;
      }
      if (other.StartTime != 0L) {
        StartTime = other.StartTime;
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
            ServerId = input.ReadInt32();
            break;
          }
          case 16: {
            StartTime = input.ReadInt64();
            break;
          }
        }
      }
    }

  }

  public sealed partial class RequestRpc : pb::IMessage<RequestRpc> {
    private static readonly pb::MessageParser<RequestRpc> _parser = new pb::MessageParser<RequestRpc>(() => new RequestRpc());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<RequestRpc> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::F1.Message.RpcReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestRpc() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestRpc(RequestRpc other) : this() {
      entityType_ = other.entityType_;
      entityId_ = other.entityId_;
      method_ = other.method_;
      args_ = other.args_;
      host_ = other.host_;
      requestId_ = other.requestId_;
      id_ = other.id_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestRpc Clone() {
      return new RequestRpc(this);
    }

    /// <summary>Field number for the "entity_type" field.</summary>
    public const int EntityTypeFieldNumber = 1;
    private int entityType_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int EntityType {
      get { return entityType_; }
      set {
        entityType_ = value;
      }
    }

    /// <summary>Field number for the "entity_id" field.</summary>
    public const int EntityIdFieldNumber = 2;
    private string entityId_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string EntityId {
      get { return entityId_; }
      set {
        entityId_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "method" field.</summary>
    public const int MethodFieldNumber = 3;
    private string method_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Method {
      get { return method_; }
      set {
        method_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "args" field.</summary>
    public const int ArgsFieldNumber = 4;
    private pb::ByteString args_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pb::ByteString Args {
      get { return args_; }
      set {
        args_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "host" field.</summary>
    public const int HostFieldNumber = 5;
    private string host_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Host {
      get { return host_; }
      set {
        host_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "request_id" field.</summary>
    public const int RequestIdFieldNumber = 6;
    private long requestId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long RequestId {
      get { return requestId_; }
      set {
        requestId_ = value;
      }
    }

    /// <summary>Field number for the "id" field.</summary>
    public const int IdFieldNumber = 7;
    private long id_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long Id {
      get { return id_; }
      set {
        id_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as RequestRpc);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(RequestRpc other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (EntityType != other.EntityType) return false;
      if (EntityId != other.EntityId) return false;
      if (Method != other.Method) return false;
      if (Args != other.Args) return false;
      if (Host != other.Host) return false;
      if (RequestId != other.RequestId) return false;
      if (Id != other.Id) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (EntityType != 0) hash ^= EntityType.GetHashCode();
      if (EntityId.Length != 0) hash ^= EntityId.GetHashCode();
      if (Method.Length != 0) hash ^= Method.GetHashCode();
      if (Args.Length != 0) hash ^= Args.GetHashCode();
      if (Host.Length != 0) hash ^= Host.GetHashCode();
      if (RequestId != 0L) hash ^= RequestId.GetHashCode();
      if (Id != 0L) hash ^= Id.GetHashCode();
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
      if (EntityType != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(EntityType);
      }
      if (EntityId.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(EntityId);
      }
      if (Method.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(Method);
      }
      if (Args.Length != 0) {
        output.WriteRawTag(34);
        output.WriteBytes(Args);
      }
      if (Host.Length != 0) {
        output.WriteRawTag(42);
        output.WriteString(Host);
      }
      if (RequestId != 0L) {
        output.WriteRawTag(48);
        output.WriteInt64(RequestId);
      }
      if (Id != 0L) {
        output.WriteRawTag(56);
        output.WriteInt64(Id);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (EntityType != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(EntityType);
      }
      if (EntityId.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(EntityId);
      }
      if (Method.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Method);
      }
      if (Args.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(Args);
      }
      if (Host.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Host);
      }
      if (RequestId != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(RequestId);
      }
      if (Id != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(Id);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(RequestRpc other) {
      if (other == null) {
        return;
      }
      if (other.EntityType != 0) {
        EntityType = other.EntityType;
      }
      if (other.EntityId.Length != 0) {
        EntityId = other.EntityId;
      }
      if (other.Method.Length != 0) {
        Method = other.Method;
      }
      if (other.Args.Length != 0) {
        Args = other.Args;
      }
      if (other.Host.Length != 0) {
        Host = other.Host;
      }
      if (other.RequestId != 0L) {
        RequestId = other.RequestId;
      }
      if (other.Id != 0L) {
        Id = other.Id;
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
            EntityType = input.ReadInt32();
            break;
          }
          case 18: {
            EntityId = input.ReadString();
            break;
          }
          case 26: {
            Method = input.ReadString();
            break;
          }
          case 34: {
            Args = input.ReadBytes();
            break;
          }
          case 42: {
            Host = input.ReadString();
            break;
          }
          case 48: {
            RequestId = input.ReadInt64();
            break;
          }
          case 56: {
            Id = input.ReadInt64();
            break;
          }
        }
      }
    }

  }

  public sealed partial class ResponseRpc : pb::IMessage<ResponseRpc> {
    private static readonly pb::MessageParser<ResponseRpc> _parser = new pb::MessageParser<ResponseRpc>(() => new ResponseRpc());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ResponseRpc> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::F1.Message.RpcReflection.Descriptor.MessageTypes[3]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponseRpc() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponseRpc(ResponseRpc other) : this() {
      id_ = other.id_;
      errorCode_ = other.errorCode_;
      errorMsg_ = other.errorMsg_;
      ret_ = other.ret_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponseRpc Clone() {
      return new ResponseRpc(this);
    }

    /// <summary>Field number for the "id" field.</summary>
    public const int IdFieldNumber = 1;
    private long id_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long Id {
      get { return id_; }
      set {
        id_ = value;
      }
    }

    /// <summary>Field number for the "error_code" field.</summary>
    public const int ErrorCodeFieldNumber = 2;
    private int errorCode_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int ErrorCode {
      get { return errorCode_; }
      set {
        errorCode_ = value;
      }
    }

    /// <summary>Field number for the "error_msg" field.</summary>
    public const int ErrorMsgFieldNumber = 3;
    private string errorMsg_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string ErrorMsg {
      get { return errorMsg_; }
      set {
        errorMsg_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "ret" field.</summary>
    public const int RetFieldNumber = 5;
    private pb::ByteString ret_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pb::ByteString Ret {
      get { return ret_; }
      set {
        ret_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ResponseRpc);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ResponseRpc other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Id != other.Id) return false;
      if (ErrorCode != other.ErrorCode) return false;
      if (ErrorMsg != other.ErrorMsg) return false;
      if (Ret != other.Ret) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Id != 0L) hash ^= Id.GetHashCode();
      if (ErrorCode != 0) hash ^= ErrorCode.GetHashCode();
      if (ErrorMsg.Length != 0) hash ^= ErrorMsg.GetHashCode();
      if (Ret.Length != 0) hash ^= Ret.GetHashCode();
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
      if (Id != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(Id);
      }
      if (ErrorCode != 0) {
        output.WriteRawTag(16);
        output.WriteInt32(ErrorCode);
      }
      if (ErrorMsg.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(ErrorMsg);
      }
      if (Ret.Length != 0) {
        output.WriteRawTag(42);
        output.WriteBytes(Ret);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Id != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(Id);
      }
      if (ErrorCode != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(ErrorCode);
      }
      if (ErrorMsg.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(ErrorMsg);
      }
      if (Ret.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(Ret);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ResponseRpc other) {
      if (other == null) {
        return;
      }
      if (other.Id != 0L) {
        Id = other.Id;
      }
      if (other.ErrorCode != 0) {
        ErrorCode = other.ErrorCode;
      }
      if (other.ErrorMsg.Length != 0) {
        ErrorMsg = other.ErrorMsg;
      }
      if (other.Ret.Length != 0) {
        Ret = other.Ret;
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
            Id = input.ReadInt64();
            break;
          }
          case 16: {
            ErrorCode = input.ReadInt32();
            break;
          }
          case 26: {
            ErrorMsg = input.ReadString();
            break;
          }
          case 42: {
            Ret = input.ReadBytes();
            break;
          }
        }
      }
    }

  }

  public sealed partial class RequestHeartBeat : pb::IMessage<RequestHeartBeat> {
    private static readonly pb::MessageParser<RequestHeartBeat> _parser = new pb::MessageParser<RequestHeartBeat>(() => new RequestHeartBeat());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<RequestHeartBeat> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::F1.Message.RpcReflection.Descriptor.MessageTypes[4]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestHeartBeat() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestHeartBeat(RequestHeartBeat other) : this() {
      milliSeconds_ = other.milliSeconds_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RequestHeartBeat Clone() {
      return new RequestHeartBeat(this);
    }

    /// <summary>Field number for the "milli_seconds" field.</summary>
    public const int MilliSecondsFieldNumber = 1;
    private long milliSeconds_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long MilliSeconds {
      get { return milliSeconds_; }
      set {
        milliSeconds_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as RequestHeartBeat);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(RequestHeartBeat other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (MilliSeconds != other.MilliSeconds) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (MilliSeconds != 0L) hash ^= MilliSeconds.GetHashCode();
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
      if (MilliSeconds != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(MilliSeconds);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (MilliSeconds != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(MilliSeconds);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(RequestHeartBeat other) {
      if (other == null) {
        return;
      }
      if (other.MilliSeconds != 0L) {
        MilliSeconds = other.MilliSeconds;
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
            MilliSeconds = input.ReadInt64();
            break;
          }
        }
      }
    }

  }

  public sealed partial class ResponseHeartBeat : pb::IMessage<ResponseHeartBeat> {
    private static readonly pb::MessageParser<ResponseHeartBeat> _parser = new pb::MessageParser<ResponseHeartBeat>(() => new ResponseHeartBeat());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ResponseHeartBeat> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::F1.Message.RpcReflection.Descriptor.MessageTypes[5]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponseHeartBeat() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponseHeartBeat(ResponseHeartBeat other) : this() {
      milliSeconds_ = other.milliSeconds_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ResponseHeartBeat Clone() {
      return new ResponseHeartBeat(this);
    }

    /// <summary>Field number for the "milli_seconds" field.</summary>
    public const int MilliSecondsFieldNumber = 1;
    private long milliSeconds_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long MilliSeconds {
      get { return milliSeconds_; }
      set {
        milliSeconds_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ResponseHeartBeat);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ResponseHeartBeat other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (MilliSeconds != other.MilliSeconds) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (MilliSeconds != 0L) hash ^= MilliSeconds.GetHashCode();
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
      if (MilliSeconds != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(MilliSeconds);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (MilliSeconds != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(MilliSeconds);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ResponseHeartBeat other) {
      if (other == null) {
        return;
      }
      if (other.MilliSeconds != 0L) {
        MilliSeconds = other.MilliSeconds;
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
            MilliSeconds = input.ReadInt64();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
