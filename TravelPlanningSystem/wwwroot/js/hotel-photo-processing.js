document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.hotel-photo-upload').forEach(input => input.addEventListener('change', async () => {
    const files = [...input.files]; if (!files.length || !window.DataTransfer) return;
    const output = new DataTransfer();
    for (const file of files) {
      if (!file.type.startsWith('image/')) { output.items.add(file); continue; }
      const image = await new Promise((resolve, reject) => { const i = new Image(); i.onload = () => resolve(i); i.onerror = reject; i.src = URL.createObjectURL(file); });
      const width = 1600, height = 1000, scale = Math.max(width / image.width, height / image.height), sourceWidth = width / scale, sourceHeight = height / scale;
      const canvas = document.createElement('canvas'); canvas.width = width; canvas.height = height;
      canvas.getContext('2d').drawImage(image, (image.width - sourceWidth) / 2, (image.height - sourceHeight) / 2, sourceWidth, sourceHeight, 0, 0, width, height);
      const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', .88)); URL.revokeObjectURL(image.src);
      if (blob) output.items.add(new File([blob], `${file.name.replace(/\.[^.]+$/, '')}.jpg`, { type: 'image/jpeg' }));
    }
    input.files = output.files;
  }));
});
