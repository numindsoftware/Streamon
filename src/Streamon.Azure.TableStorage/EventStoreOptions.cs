using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamon.Azure.TableStorage;

public record EventStoreOptions(string EntityFieldPrefix = "_ef_", string MetadataFieldPrefix = "_mt_");
