using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.shared;

public record UploaderDto
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = null!;
    public int BattleNetId { get; init; }
    public ICollection<PlayerUploadDto> Players { get; init; } = null!;
}

public record PlayerUploadDto
{
    public string Name { get; init; } = null!;
    public int Toonid { get; init; }
}
