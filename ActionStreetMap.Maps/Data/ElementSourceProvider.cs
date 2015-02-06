﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ActionStreetMap.Core;
using ActionStreetMap.Infrastructure.Config;
using ActionStreetMap.Infrastructure.Dependencies;
using ActionStreetMap.Infrastructure.Diagnostic;
using ActionStreetMap.Infrastructure.Formats.Json;
using ActionStreetMap.Infrastructure.IO;
using ActionStreetMap.Infrastructure.Primitives;
using ActionStreetMap.Infrastructure.Reactive;
using ActionStreetMap.Maps.Data.Import;
using ActionStreetMap.Maps.Data.Spatial;
using ActionStreetMap.Infrastructure.Utilities;

namespace ActionStreetMap.Maps.Data
{
    /// <summary> Provides the way to get the corresponding element source by geocoordinate. </summary>
    public interface IElementSourceProvider: IDisposable
    {
        /// <summary> Returns element sources by query represented by bounding box. </summary>
        /// <returns>Element source.</returns>
        IObservable<IElementSource> Get(BoundingBox query);

        /// <summary> Returns active element sources. </summary>
        /// <returns>Element source.</returns>
        IObservable<IElementSource> Get();
    }

    /// <summary> Default implementation of <see cref="IElementSourceProvider"/>. </summary>
    internal sealed class ElementSourceProvider : IElementSourceProvider, IConfigurable
    {
        private const string LogTag = "mapdata.source";
        private readonly Regex _geoCoordinateRegex = new Regex(@"([-+]?\d{1,2}([.]\d+)?),\s*([-+]?\d{1,3}([.]\d+)?)");
        private readonly string[] _splitString= { " " };

        private string _mapDataServerUri;
        private string _mapDataServerQuery;
        private string _mapDataFormat;
        private string _indexSettingsPath;

        private readonly IPathResolver _pathResolver;
        private readonly IFileSystemService _fileSystemService;
        private readonly IObjectPool _objectPool;
        private IndexSettings _settings;
        private RTree<string> _tree;
        private MutableTuple<string, IElementSource> _elementSourceCache;

        /// <summary> Trace. </summary>
        [Dependency]
        public ITrace Trace { get; set; }

        /// <summary> Creates instance of <see cref="ElementSourceProvider"/>. </summary>
        /// <param name="pathResolver">Path resolver.</param>
        /// <param name="fileSystemService">File system service.</param>
        /// <param name="objectPool">Object pool.</param>
        [Dependency]
        public ElementSourceProvider(IPathResolver pathResolver, IFileSystemService fileSystemService, IObjectPool objectPool)
        {
            _pathResolver = pathResolver;
            _fileSystemService = fileSystemService;
            _objectPool = objectPool;
        }

        /// <inheritdoc />
        public IObservable<IElementSource> Get()
        {
            return _elementSourceCache != null ? 
                Observable.Return<IElementSource>(_elementSourceCache.Item2) : 
                Observable.Empty<IElementSource>();
        }

        /// <inheritdoc />
        public IObservable<IElementSource> Get(BoundingBox query)
        {
            // NOTE block thread here
            Trace.Info(LogTag, "getting element sources for {0}", query);
            var elementSourcePath = _tree.Search(query).Wait();
            if (elementSourcePath == null && !String.IsNullOrEmpty(_mapDataServerUri))
            {
                var queryString = String.Format(_mapDataServerQuery, query.MinPoint.Longitude, query.MinPoint.Latitude, 
                    query.MaxPoint.Longitude, query.MaxPoint.Latitude);
                var uri = String.Format("{0}{1}", _mapDataServerUri, Uri.EscapeDataString(queryString));
                Trace.Warn(LogTag, Strings.NoPresistentElementSourceFound, query, uri);
                return ObservableWWW.GetAndGetBytes(uri)
                    .Take(1)
                    .SelectMany(bytes =>
                    {
                        Trace.Info(LogTag, "build index from {0} bytes received", bytes.Length);
                        if (_settings == null) ReadIndexSettings();
                        var indexBuilder = new InMemoryIndexBuilder(_mapDataFormat, new MemoryStream(bytes), _settings, _objectPool, Trace);
                        indexBuilder.Build();
                        var elementStore = new ElementSource(indexBuilder.KvUsage, indexBuilder.KvIndex, 
                            indexBuilder.KvStore, indexBuilder.Store, indexBuilder.Tree);
                        SetCurrentElementSource(query.ToString(), elementStore);
                        return Observable.Return<IElementSource>(elementStore);
                    });
            }

            if (_elementSourceCache == null || elementSourcePath != _elementSourceCache.Item1)
            {
                Trace.Info(LogTag, "load index data from {0}", elementSourcePath);
                SetCurrentElementSource(elementSourcePath, new ElementSource(elementSourcePath, _fileSystemService, _objectPool));
            }

            return Observable.Return(_elementSourceCache.Item2);
        }

        private void SetCurrentElementSource(string elementSourcePath, IElementSource elementSource)
        {
            if (_elementSourceCache != null)
                _elementSourceCache.Item2.Dispose();
            _elementSourceCache = new MutableTuple<string, IElementSource>(elementSourcePath, elementSource);
        }

        private void ReadIndexSettings() 
        {
            var jsonContent = _fileSystemService.ReadText(_indexSettingsPath);
            var node = JSON.Parse(jsonContent);
            _settings = new IndexSettings();
            _settings.ReadFromJson(node);
        }

        private void SearchAndReadMapIndexHeaders(string folder)
        {
            _fileSystemService.GetFiles(folder, Consts.HeaderFileName).ToList()
                .ForEach(ReadMapIndexHeader);

            _fileSystemService.GetDirectories(folder, "*").ToList()
                .ForEach(SearchAndReadMapIndexHeaders);
        }

        private void ReadMapIndexHeader(string headerPath)
        {
            using (var reader = new StreamReader(_fileSystemService.ReadStream(headerPath)))
            {
                var str = reader.ReadLine();
                var coordinateStrings = str.Split(_splitString, StringSplitOptions.None);
                var minPoint = GetCoordinateFromString(coordinateStrings[0]);
                var maxPoint = GetCoordinateFromString(coordinateStrings[1]);

                var envelop = new Envelop(minPoint, maxPoint);
                _tree.Insert(Path.GetDirectoryName(headerPath), envelop);
            }
        }

        private GeoCoordinate GetCoordinateFromString(string coordinateStr)
        {
            var coordinates = _geoCoordinateRegex.Match(coordinateStr).Value.Split(',');

            var latitude = double.Parse(coordinates[0]);
            var longitude = double.Parse(coordinates[1]);

            return new GeoCoordinate(latitude, longitude);
        }

        /// <inheritdoc />
        public void Configure(IConfigSection configSection)
        {
            // @"http://api.openstreetmap.org/api/0.6/map?bbox="
            _mapDataServerUri = configSection.GetString(@"remote.server", null);
            //api/0.6/map?bbox=left,bottom,right,top
            _mapDataServerQuery = configSection.GetString(@"remote.query", null);
            _mapDataFormat = configSection.GetString(@"remote.format", "xml");
            _indexSettingsPath = configSection.GetString(@"index.settings", null);

            _tree = new RTree<string>();
            var rootFolder = configSection.GetString("local", null);
            if (!String.IsNullOrEmpty(rootFolder))
                SearchAndReadMapIndexHeaders(_pathResolver.Resolve(rootFolder));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if(_elementSourceCache != null)
                _elementSourceCache.Item2.Dispose();
        }
    }
}
