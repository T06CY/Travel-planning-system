SELECT
    hr.HotelRoomId,
    hr.HotelName,
    hp.HotelRoomPhotoId,
    hp.PhotoUrl,
    hp.Caption
FROM dbo.HotelRooms hr
LEFT JOIN dbo.HotelRoomPhotos hp
    ON hr.HotelRoomId = hp.HotelRoomId
ORDER BY hr.HotelRoomId;