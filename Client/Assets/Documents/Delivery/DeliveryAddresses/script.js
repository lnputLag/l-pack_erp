ymaps.ready(init);

function init() {
    var Latitude = '52.589638';
    var Longtitude = '39.583733';
    var iconCaptionName = 'Здесь?';
    var LatName = 'Широта= ';
    var LongName = ', долгота=';
    var LengthDistance = 0;

    var myPlacemark;

    // Создание метки.
    function createPlacemark(coords) {
        return new ymaps.Placemark(coords, {
            iconCaption: iconCaptionName
        }, {
            preset: 'islands#violetDotIconWithCaption',
            draggable: false
        });
    }

    // создаем карту
    myMap = new ymaps.Map('map', {
        center: [Latitude, Longtitude],
        zoom: 16, controls: ['smallMapDefaultSet']
    }, {
        searchControlProvider: 'yandex#search'
    });

    // Создадим панель маршрутизации.
    routePanelControl = new ymaps.control.RoutePanel({
        options: {
            // Добавим заголовок панели.
            showHeader: false,
            title: '',
            visible: false,
        }
    });
    // Пользователь сможет построить только автомобильный маршрут.
    routePanelControl.routePanel.options.set({
        types: { auto: true }
    });

    // неизменяемая точка "откуда"
    routePanelControl.routePanel.state.set({
        fromEnabled: false,
        from: 'Липецк, ул. Ковалева, 125а'
    });

    // Создание метки.
    myPlacemark = createPlacemark([Latitude, Longtitude]);
    myMap.geoObjects.add(myPlacemark); getAddress(myPlacemark.geometry.getCoordinates());

    // Добавим элемент управления на карту и сразу переведем
    // её в «полноэкранный режим».

    var fullscreenControl = new ymaps.control.FullscreenControl();
    myMap.controls.add(fullscreenControl);
    fullscreenControl.enterFullscreen();
    myMap.controls.remove(fullscreenControl);
    myMap.controls.remove('fullscreenControl');
    myMap.controls.remove('geolocationControl');
    myMap.controls.remove('typeSelector');

    // Слушаем клик на карте.
    myMap.events.add('click', function (e) {
        var coords = e.get('coords');

        // Если метка уже создана – просто передвигаем ее.
        if (myPlacemark) {
            myPlacemark.geometry.setCoordinates(coords);
        }
        // Если нет – создаем.
        else {
            myPlacemark = createPlacemark(coords);
            myMap.geoObjects.add(myPlacemark);
            // Слушаем событие окончания перетаскивания на метке.
            myPlacemark.events.add('dragend', function () { getAddress(myPlacemark.geometry.getCoordinates()); });
        }
        getAddress(coords);
    });

    // Слушаем событие окончания перетаскивания на метке.
    // myPlacemark.events.add('dragend', function () { getAddress(myPlacemark.geometry.getCoordinates()); });

    // Определяем адрес по координатам (обратное геокодирование).
    function getAddress(coords) {
        myPlacemark.properties.set('iconCaption', '');
        ymaps.geocode(coords).then(function (res) {
            var firstGeoObject = res.geoObjects.get(0);


            // изменяемая точка "куда"
            routePanelControl.routePanel.state.set({
                toEnabled: false,
                to: firstGeoObject.getAddressLine()
            });

            myMap.controls.add(routePanelControl);

            // Получим ссылку на маршрут.
            routePanelControl.routePanel.getRouteAsync().then(function (route) {
                // Зададим максимально допустимое число маршрутов, возвращаемых мультимаршрутизатором.
                route.model.setParams({ results: 1 }, true);
                // Повесим обработчик на событие построения маршрута.
                route.model.events.add('requestsuccess', function () {
                    var activeRoute = route.getActiveRoute();
                    // Получим протяженность маршрута.
                    LengthDistance = route.getActiveRoute().properties.get("distance");
                    console.log('FullAdres=' + firstGeoObject.getAddressLine() + ' Lat=' + coords[0] + ' Long=' + coords[1] + ', Distance= ' + LengthDistance.value / 1000);
                });
                myMap.controls.remove(routePanelControl);
            });

            myPlacemark.properties
                .set({
                    // Формируем строку с данными об объекте.
                    iconCaption: [
                        // Название населенного пункта или вышестоящее административно-территориальное образование.
                        firstGeoObject.getLocalities().length ? firstGeoObject.getLocalities() : firstGeoObject.getAdministrativeAreas(),
                        // Получаем путь до топонима, если метод вернул null, запрашиваем наименование здания.
                        firstGeoObject.getThoroughfare() || firstGeoObject.getPremise()
                    ].filter(Boolean).join(', '),
                    // В качестве контента балуна задаем строку с адресом объекта.
                    balloonContent: firstGeoObject.getAddressLine() + '.\n' + LatName + coords[0] + LongName + coords[1]
                });
        });
    }
}	

function goToPoint(coords) {
    // Если метка уже создана – просто передвигаем ее.
    if (myPlacemark) {
        myPlacemark.geometry.setCoordinates(coords);
    }
    // Если нет – создаем.
    else {
        myPlacemark = createPlacemark(coords);
        myMap.geoObjects.add(myPlacemark);
        // Слушаем событие окончания перетаскивания на метке.
        myPlacemark.events.add('dragend', function () { getAddress(myPlacemark.geometry.getCoordinates()); });
    }
    getAddress(coords);
}