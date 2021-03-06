﻿//using DotnetSpider.Core.Infrastructure;
//using DotnetSpider.Core.Redial;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Text;
//using Polly.Retry;
//using Polly;
//using DotnetSpider.Core;
//using System.Security.Cryptography;
//using System.IO;
//using MessagePack;
//using Serilog;

//namespace DotnetSpider.Extension.Pipeline
//{
//	/// <summary>
//	/// 通过HTTP上传数据到企业服务
//	/// </summary>
//	public class HttpMySqlEntityPipeline : MySqlEntityPipeline
//	{
//		private readonly string _api;
//		private readonly RetryPolicy _retryPolicy;
//		private readonly ICryptoTransform _cryptoTransform;

//		/// <summary>
//		/// 构造方法
//		/// </summary>
//		/// <param name="api">上传的API</param>
//		public HttpMySqlEntityPipeline(string api = null)
//		{
//			if (string.IsNullOrWhiteSpace(api))
//			{
//				_api = Env.HubServicePipelineUrl;
//			}
//			else
//			{
//				_api = api;
//			}

//			_retryPolicy = Policy.Handle<Exception>().Retry(5, (ex, count) =>
//			{
//				Log.Logger.Error($"Pipeline execute error [{count}]: {ex}");
//			});

//			DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
//			var bytes = Encoding.ASCII.GetBytes(Env.SqlEncryptCode);
//			_cryptoTransform = cryptoProvider.CreateEncryptor(bytes, bytes);
//		}

//		internal override void InitDatabaseAndTable()
//		{
//			_retryPolicy.Execute(() =>
//			{
//				NetworkCenter.Current.Execute("httpPipeline", () =>
//				{
//					foreach (var adapter in EntityAdapters.Values)
//					{
//						var sql = GenerateIfDatabaseExistsSql(adapter);

//						if (ExecuteHttpSql(sql) == 0)
//						{
//							sql = GenerateCreateDatabaseSql(adapter);
//							ExecuteHttpSql(sql);
//						}

//						sql = GenerateCreateTableSql(adapter);
//						ExecuteHttpSql(sql);
//					}
//				});
//			});
//		}

//		/// <summary>
//		/// 通过HTTP上传数据到企业服务
//		/// </summary>
//		/// <param name="entityName">爬虫实体类的名称</param>
//		/// <param name="datas">实体类数据</param>
//		/// <param name="spider">爬虫</param>
//		/// <returns>最终影响结果数量(如数据库影响行数)</returns>
//		public override int Process(string entityName, IEnumerable<dynamic> datas, ISpider spider)
//		{
//			int count = 0;

//			if (EntityAdapters.TryGetValue(entityName, out var metadata))
//			{
//				string sql = string.Empty;

//				switch (metadata.PipelineMode)
//				{
//					case PipelineMode.Insert:
//						{
//							sql = metadata.InsertSql;
//							break;
//						}
//					case PipelineMode.InsertAndIgnoreDuplicate:
//						{
//							sql = metadata.InsertAndIgnoreDuplicateSql;
//							break;
//						}
//					case PipelineMode.InsertNewAndUpdateOld:
//						{
//							sql = metadata.InsertNewAndUpdateOldSql;
//							break;
//						}
//					case PipelineMode.Update:
//						{
//							sql = metadata.UpdateSql;
//							break;
//						}
//					default:
//						{
//							sql = metadata.InsertSql;
//							break;
//						}
//				}

//				count = ExecuteHttpSql(sql, datas);
//			}
//			return count;
//		}

//		private int ExecuteHttpSql(string sql, dynamic data = null)
//		{
//			MemoryStream ms = new MemoryStream();
//			CryptoStream cst = new CryptoStream(ms, _cryptoTransform, CryptoStreamMode.Write);

//			StreamWriter sw = new StreamWriter(cst);
//			sw.Write(sql);
//			sw.Flush();
//			cst.FlushFinalBlock();
//			sw.Flush();

//			string cryptoSql = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
//			var json = JsonConvert.SerializeObject(new
//			{
//				Sql = cryptoSql,
//				Dt = data,
//				D = Core.Infrastructure.Database.Database.MySql
//			});

//			var encodingBytes = Encoding.UTF8.GetBytes(json);
//			var bytes = LZ4MessagePackSerializer.ToLZ4Binary(new ArraySegment<byte>(encodingBytes));
//			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _api);
//			httpRequestMessage.Headers.Add("DotnetSpiderToken", Env.HubServiceToken);
//			httpRequestMessage.Content = new ByteArrayContent(bytes);
//			var response = HttpSender.Client.SendAsync(httpRequestMessage).Result;
//			response.EnsureSuccessStatusCode();

//			return Convert.ToInt16(response.Content.ReadAsStringAsync().Result);
//		}
//	}
//}
