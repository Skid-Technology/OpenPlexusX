using System;
using System.IO;
using System.Reflection;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace VibranceHud;

internal sealed class DxShader : IDisposable
{
	private readonly VertexShader _vs;

	private readonly PixelShader _ps;

	private readonly InputLayout _layout;

	private readonly SharpDX.Direct3D11.Buffer _matrixBuffer;

	private readonly SharpDX.Direct3D11.Buffer _vertexBuffer;

	private readonly SamplerState _sampler;

	private readonly DeviceContext _ctx;

	private const int VertexStride = 24;

	public DxShader(SharpDX.Direct3D11.Device device, DeviceContext ctx)
	{
		_ctx = ctx;
		using (ShaderBytecode shaderBytecode = new ShaderBytecode(LoadEmbeddedHLSL()))
		{
			_ps = new PixelShader(device, shaderBytecode);
		}
		using (CompilationResult compilationResult = ShaderBytecode.Compile("struct VS_IN { float4 pos:POSITION; float2 uv:TEXCOORD; }; struct VS_OUT { float4 pos:SV_POSITION; float2 uv:TEXCOORD; }; VS_OUT main(VS_IN i) { VS_OUT o; o.pos = i.pos; o.uv = i.uv; return o; }", "main", "vs_5_0"))
		{
			_vs = new VertexShader(device, compilationResult);
			_layout = new InputLayout(device, compilationResult, new InputElement[2]
			{
				new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
				new InputElement("TEXCOORD", 0, Format.R32G32_Float, 16, 0)
			});
		}
		_matrixBuffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
		{
			SizeInBytes = 80,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.ConstantBuffer,
			CpuAccessFlags = CpuAccessFlags.None
		});
		float[] data = new float[36]
		{
			-1f, 1f, 0f, 1f, 0f, 0f, 1f, 1f, 0f, 1f,
			1f, 0f, -1f, -1f, 0f, 1f, 0f, 1f, 1f, 1f,
			0f, 1f, 1f, 0f, 1f, -1f, 0f, 1f, 1f, 1f,
			-1f, -1f, 0f, 1f, 0f, 1f
		};
		_vertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, data);
		_sampler = new SamplerState(device, new SamplerStateDescription
		{
			Filter = Filter.MinMagMipLinear,
			AddressU = TextureAddressMode.Clamp,
			AddressV = TextureAddressMode.Clamp,
			AddressW = TextureAddressMode.Clamp,
			ComparisonFunction = Comparison.Never,
			MinimumLod = 0f,
			MaximumLod = float.MaxValue
		});
	}

	public bool ApplyMatrix(float[] matrix)
	{
		_ctx.UpdateSubresource(PackForShader(matrix), _matrixBuffer);
		return true;
	}

	internal static float[] PackForShader(float[] matrix)
	{
		float[] array = new float[20];
		for (int i = 0; i < 3; i++)
		{
			array[i * 4] = matrix[i];
			array[i * 4 + 1] = matrix[5 + i];
			array[i * 4 + 2] = matrix[10 + i];
			array[i * 4 + 3] = matrix[20 + i];
		}
		return array;
	}

	public void Bind(ShaderResourceView capturedFrame)
	{
		_ctx.InputAssembler.InputLayout = _layout;
		_ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		_ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, 24, 0));
		_ctx.VertexShader.Set(_vs);
		_ctx.PixelShader.Set(_ps);
		_ctx.PixelShader.SetConstantBuffer(0, _matrixBuffer);
		_ctx.PixelShader.SetSampler(0, _sampler);
		_ctx.PixelShader.SetShaderResource(0, capturedFrame);
	}

	public void Draw()
	{
		_ctx.Draw(6, 0);
	}

	public void Dispose()
	{
		_vs.Dispose();
		_ps.Dispose();
		_layout.Dispose();
		_matrixBuffer.Dispose();
		_vertexBuffer.Dispose();
		_sampler.Dispose();
	}

	private static byte[] LoadEmbeddedHLSL()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlexusX.saturation.hlsl");
		if (stream == null)
		{
			throw new InvalidOperationException("saturation.hlsl not embedded");
		}
		using StreamReader streamReader = new StreamReader(stream);
		using CompilationResult compilationResult = ShaderBytecode.Compile(streamReader.ReadToEnd(), "main", "ps_5_0");
		byte[] array = new byte[compilationResult.Bytecode.Data.Length];
		Array.Copy(compilationResult.Bytecode.Data, array, array.Length);
		return array;
	}
}
